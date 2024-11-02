using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

/// <summary>
/// Deletes and updates users in Multifactor Cloud.
/// </summary>
public interface ISynchronizeUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}

internal class SynchronizeUsers : ISynchronizeUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly IGetReferenceGroup _getReferenceGroup;
    private readonly IApplicationStorage _storage;
    private readonly Deleter _deleter;
    private readonly Updater _updater;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeUsers> _logger;

    public SynchronizeUsers(RequiredLdapAttributes requiredLdapAttributes,
        IGetReferenceGroup getReferenceGroup,
        IApplicationStorage storage,
        Deleter deleter,
        Updater updater,
        CodeTimer codeTimer,
        ILogger<SynchronizeUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroup = getReferenceGroup;
        _storage = storage;
        _deleter = deleter;
        _updater = updater;
        _codeTimer = codeTimer;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogInformation(ApplicationEvent.StartUserSynchronization, "Start users synchronization for group {group}", groupGuid);
        
        var names = _requiredLdapAttributes.GetNames().ToArray();
        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", names));

        var getGroupTimer = _codeTimer.Start("Get Reference Group");
        var referenceGroup = _getReferenceGroup.Execute(groupGuid, names);
        getGroupTimer.Stop();
        _logger.LogDebug("Reference group found: {Group:l}", referenceGroup);
        
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug("Users synchronizing skipping because group doesn't exist in cache storage");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
            return;
        }

        if (ReferenceGroupHasDifferentCountOfMembers(referenceGroup, cachedGroup))
        {
            _logger.LogDebug("Reference and cached groups are different");
            _logger.LogDebug("Searching for deleted members...");
            
            var deleted = GetDeletedMemberGuids(referenceGroup, cachedGroup).ToArray();
            if (deleted.Length == 0)
            {
                _logger.LogDebug("Deleted members was not found");
            }
            else
            {
                _logger.LogDebug("Found deleted users: {Deleted}", deleted.Length);

                await _deleter.DeleteManyAsync(cachedGroup, deleted, token);

                _logger.LogDebug("Deleted members are synchronized");
            }
        }

        _logger.LogDebug("Searching for existed but modified members...");
        var modifiedMembers = MemberChangeDetector.GetModifiedMembers(referenceGroup, cachedGroup).ToArray();
        if (modifiedMembers.Length == 0)
        {
            _logger.LogDebug("Modified members was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
            return;
        }

        _logger.LogDebug("Found modified users: {Modified}", modifiedMembers.Length);
        await _updater.UpdateManyAsync(cachedGroup, modifiedMembers, token);

        var updateModifiedTimer = _codeTimer.Start("Update Cached Group: Modified Users");
        _storage.UpdateGroup(cachedGroup);
        updateModifiedTimer.Stop();
        _logger.LogDebug("Modified members are synchronized");
        
        _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
    }

    private static bool ReferenceGroupHasDifferentCountOfMembers(ReferenceDirectoryGroup refGroup, CachedDirectoryGroup cachedGroup)
    {
        var referenceMembersHash = EntriesHash.Create(refGroup.Members.Select(x => x.Guid));
        return referenceMembersHash != cachedGroup.Hash;
    }

    private static IEnumerable<DirectoryGuid> GetDeletedMemberGuids(ReferenceDirectoryGroup referenceGroup, 
        CachedDirectoryGroup cachedGroup)
    {
        var refMemberGuids = referenceGroup.Members.Select(x => x.Guid);
        var cachedMemberGuids = cachedGroup.Members.Select(x => x.Id);
        return cachedMemberGuids.Except(refMemberGuids);
    }
}
