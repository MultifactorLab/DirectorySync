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
    private readonly ILdapMemberPort _memberPort;
    private readonly IUserUpdater _userUpdater;
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeGroupsUseCase> _logger;

    public SynchronizeUsersUseCase(IMemberDatabase memberDatabase,
        ILdapMemberPort memberPort,
        IUserUpdater userUpdater,
        ISyncSettingsOptions syncSettingsOptions,
        CodeTimer codeTimer,
        ILogger<SynchronizeGroupsUseCase> logger)
    {
        _memberDatabase = memberDatabase;
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
        
        var memberIds = cachedMembers.Select(m => m.Id).ToArray();
        
        var getUsersTimer = _codeTimer.Start("Get Reference Members");
        var freshEntries = _memberPort.GetByGuids(memberIds, requiredNames, cancellationToken);
        getUsersTimer.Stop();
        _logger.LogDebug("Reference users found: {@Users}", freshEntries);
        
        var referenceMemberMap = freshEntries.ToDictionary(x => x.Id);
        
        var modifiedMembers = ProcessMembersChanges(cachedMembers, referenceMemberMap);

        if (modifiedMembers.Count == 0)
        {
            _logger.LogDebug("Modified users was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization");
            return;
        }
        
        _logger.LogDebug("Found modified users: {@Modified}", modifiedMembers);
        
        var updatedMembers = await _userUpdater.UpdateManyAsync(modifiedMembers, cancellationToken);
        
        _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization. Updated users: {Users:l}", updatedMembers.Select(c => c.Identity));
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
                cached.MarkForUpdate();
                changed.Add(cached);
            }
        }
        
        return changed.AsReadOnly();
    }
}
