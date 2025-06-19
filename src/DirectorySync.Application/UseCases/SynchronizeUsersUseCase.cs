using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Cloud;
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
    private readonly ILogger<SynchronizeGroupsUseCase> _logger;

    public SynchronizeUsersUseCase(IMemberDatabase memberDatabase,
        ILdapMemberPort memberPort,
        IUserUpdater userUpdater,
        ISyncSettingsOptions syncSettingsOptions,
        ILogger<SynchronizeGroupsUseCase> logger)
    {
        _memberDatabase = memberDatabase;
        _memberPort = memberPort;
        _userUpdater = userUpdater;
        _syncSettingsOptions = syncSettingsOptions;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cachedMembers = _memberDatabase.FindAll();
        if (cachedMembers.Count == 0)
        {
            return;
        }

        var requiredNames = _syncSettingsOptions.GetRequiredAttributeNames();
        
        var memberIds = cachedMembers.Select(m => m.Id).ToArray();
        var freshEntries = await _memberPort.GetByGuidsAsync(memberIds, requiredNames, cancellationToken);
        
        var referenceMemberMap = freshEntries.ToDictionary(x => x.Id);
        
        var changed = ProcessMembersChanges(cachedMembers, referenceMemberMap);
        
        var toUpdate = await _userUpdater.UpdateManyAsync(changed, cancellationToken);
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
                cached.SetNewAttributes(referenceMember.Attributes);
                cached.MarkForUpdate();
                changed.Add(cached);
            }
        }
        
        return changed.AsReadOnly();
    }
}
