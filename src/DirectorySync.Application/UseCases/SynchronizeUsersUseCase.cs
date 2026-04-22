using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.UseCases;

public interface ISynchronizeUsersUseCase
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public class SynchronizeUsersUseCase : ISynchronizeUsersUseCase
{
    private readonly IMemberDatabase _memberDatabase;
    private readonly IDirectoryDomainDatabase _directoryDomainDatabase;
    private readonly ILdapMemberPort _memberPort;
    private readonly IUserUpdater _userUpdater;
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeUsersUseCase> _logger;

    public SynchronizeUsersUseCase(IMemberDatabase memberDatabase,
        IDirectoryDomainDatabase directoryDomainDatabase,
        ILdapMemberPort memberPort,
        IUserUpdater userUpdater,
        ISyncSettingsOptions syncSettingsOptions,
        CodeTimer codeTimer,
        ILogger<SynchronizeUsersUseCase> logger)
    {
        _memberDatabase = memberDatabase;
        _directoryDomainDatabase = directoryDomainDatabase;
        _memberPort = memberPort;
        _userUpdater = userUpdater;
        _syncSettingsOptions = syncSettingsOptions;
        _codeTimer = codeTimer; 
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(ApplicationEvent.StartUserSynchronization, "Start users synchronization");
        
        var requiredNames = _syncSettingsOptions.GetRequiredAttributeNames();
        if (requiredNames.Length == 0)
        {
            _logger.LogWarning(ApplicationEvent.InvalidServiceConfiguration, "Required LDAP attributes not defined. Please check attribute mapping");
            return;
        }
        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", requiredNames));
        
        var cachedMembers = _memberDatabase.FindAll();
        if (cachedMembers.Count == 0)
        {
            _logger.LogDebug("Users in cache not found");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization");
            return;
        }
        
        var searchDomains = _directoryDomainDatabase.FindAll();

        if (searchDomains.Count == 0)
        {
            _logger.LogDebug("No search domains configured; skipping user synchronization");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization");
            return;
        }
        
        var memberIds = cachedMembers.Select(m => m.Id).ToArray();
        
        var getUsersTimer = _codeTimer.Start("Get directory users");
        var freshEntries = _memberPort.GetByGuids(memberIds, 
            requiredNames, 
            searchDomains.ToArray());
        
        getUsersTimer.Stop();
        _logger.LogDebug("Directory users found: {Users}", freshEntries.Count);
        
        var referenceMemberMap = freshEntries.ToDictionary(x => x.Id);
        
        var modifiedMembers = ProcessMembersChanges(cachedMembers, referenceMemberMap);

        if (modifiedMembers.Count == 0)
        {
            _logger.LogDebug("Modified users was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization");
            return;
        }
        
        _logger.LogDebug("Modified users pending sync: {Count}", modifiedMembers.Count);
        _logger.LogTrace("Modified users detail: {@Members}", modifiedMembers);
        
        var updatedMembers = await _userUpdater.UpdateManyAsync(modifiedMembers, cancellationToken);
        
        _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization,
            "Complete users synchronization. Updated count: {Count}", updatedMembers.Count);
        _logger.LogTrace("Updated identities: {Identities:l}", string.Join(",", updatedMembers.Select(c => c.Identity)));
    }

    private ReadOnlyCollection<MemberModel> ProcessMembersChanges(IEnumerable<MemberModel> cachedMembers,
        Dictionary<DirectoryGuid, MemberModel> referenceMemberMap)
    {
        var changed = new List<MemberModel>();

        foreach (var cached in cachedMembers)
        {
            if (!referenceMemberMap.TryGetValue(cached.Id, out var referenceMember))
            {
                continue;
            }

            if (cached.AttributesHash != referenceMember.AttributesHash)
            {
                cached.SetProperties(referenceMember.Properties, referenceMember.AttributesHash);

                if (cached.Identity != referenceMember.Identity)
                {
                    _logger.LogInformation(ApplicationEvent.UserLoginChanged,
                        "User login change detected: {OldLogin} -> {NewLogin} (externalObjectId: {ExternalObjectId})",
                        cached.Identity, referenceMember.Identity, ExternalDirectoryObjectId.ToCanonicalString(cached.Id));
                    cached.MarkForIdentityUpdate(referenceMember.Identity);
                }
                else
                {
                    cached.MarkForUpdate();
                }

                changed.Add(cached);
            }
        }
        
        return changed.AsReadOnly();
    }
}
