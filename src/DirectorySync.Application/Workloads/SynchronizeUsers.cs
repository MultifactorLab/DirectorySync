using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Application.Measuring;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

internal class SynchronizeUsers : ISynchronizeUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly IGetReferenceGroup _getReferenceGroup;
    private readonly IApplicationStorage _storage;
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeUsers> _logger;

    public SynchronizeUsers(RequiredLdapAttributes requiredLdapAttributes,
        IGetReferenceGroup getReferenceGroup,
        IApplicationStorage storage,
        MultifactorPropertyMapper propertyMapper,
        IMultifactorApi api,
        CodeTimer codeTimer,
        ILogger<SynchronizeUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroup = getReferenceGroup;
        _storage = storage;
        _propertyMapper = propertyMapper;
        _api = api;
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

        var referenceMembersHash = new EntriesHash(referenceGroup.Members.Select(x => x.Guid));
        if (referenceMembersHash != cachedGroup.Hash)
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

                await DeleteAsync(cachedGroup, deleted, token);

                var updateDeletedTimer = _codeTimer.Start("Update Cached Group: Deleted Users");
                _storage.UpdateGroup(cachedGroup);
                updateDeletedTimer.Stop();
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
        await UpdateAsync(cachedGroup, modifiedMembers, token);

        var updateModifiedTimer = _codeTimer.Start("Update Cached Group: Modified Users");
        _storage.UpdateGroup(cachedGroup);
        updateModifiedTimer.Stop();
        _logger.LogDebug("Modified members are synchronized");
        
        _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
    }

    private static IEnumerable<DirectoryGuid> GetDeletedMemberGuids(ReferenceDirectoryGroup referenceGroup, 
        CachedDirectoryGroup cachedGroup)
    {
        var refMemberGuids = referenceGroup.Members.Select(x => x.Guid);
        var cachedMemberGuids = cachedGroup.Members.Select(x => x.Guid);
        return refMemberGuids.Except(cachedMemberGuids);
    }

    private async Task DeleteAsync(CachedDirectoryGroup group, 
        DirectoryGuid[] deletedUsers,
        CancellationToken token)
    {
        var mfIds = group.Members
            .Where(x => deletedUsers.Contains(x.Guid))
            .Select(x => x.Identity);

        var bucket = new DeletedUsersBucket();
        foreach (var id in mfIds)
        {
            bucket.AddDeletedUser(id);
        }

        var deleteApiTimer = _codeTimer.Start("Api Request: Delete Users");
        var res = await _api.DeleteManyAsync(bucket, token);
        deleteApiTimer.Stop();
        
        foreach (var id in res.DeletedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.Identity == id);
            if (cachedUser is not null)
            {
                group.DeleteMembers(cachedUser.Guid);
            }
        }
    }

    private async Task UpdateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryGroupMember[] modified,
        CancellationToken token)
    {
        var bucket = new ModifiedUsersBucket();
        foreach (ReferenceDirectoryGroupMember member in modified)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);
            
            var props = _propertyMapper.Map(member.Attributes);
            
            var cachedMember = group.Members.First(x => x.Guid == member.Guid);
            var user = bucket.AddModifiedUser(cachedMember.Identity);

            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }
        }

        var updateApiTimer = _codeTimer.Start("Api Request: Update Users");
        var res = await _api.UpdateManyAsync(bucket, token);
        updateApiTimer.Stop();

        foreach (var id in res.UpdatedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.Identity == id);
            if (cachedUser is null)
            {
                continue;
            }
            
            var refMember = modified.First(x => x.Guid == cachedUser.Guid);
            var hash = new AttributesHash(refMember.Attributes);
            cachedUser.UpdateHash(hash);
        }
    }
}
