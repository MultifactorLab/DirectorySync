using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.UseCases;

public interface ISynchronizeGroupsUseCase
{
    Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids, CancellationToken cancellationToken = default);
}

public class SynchronizeGroupsUseCase : ISynchronizeGroupsUseCase
{
    private readonly IGroupDatabase _groupDatabase;
    private readonly IMemberDatabase _memberDatabase;
    private readonly ILdapGroupPort _groupPort;
    private readonly ILdapMemberPort _memberPort;
    private readonly IUserGroupsMapper _userGroupsMapper;
    private readonly IUserCreator _userCreator;
    private readonly IUserUpdater _userUpdater;
    private readonly IUserDeleter _userDeleter;
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly ILogger<SynchronizeGroupsUseCase> _logger;

    public SynchronizeGroupsUseCase(IGroupDatabase groupDatabase,
        IMemberDatabase memberDatabase,
        ILdapGroupPort groupPort,
        ILdapMemberPort memberPort,
        IUserGroupsMapper userGroupsMapper,
        IUserCreator userCreator,
        IUserUpdater userUpdater,
        IUserDeleter userDeleter,
        ISyncSettingsOptions syncSettingsOptions,
        ILogger<SynchronizeGroupsUseCase> logger)
    {
        _groupDatabase = groupDatabase;
        _memberDatabase = memberDatabase;
        _groupPort = groupPort;
        _memberPort = memberPort;
        _userGroupsMapper = userGroupsMapper;
        _userCreator = userCreator;
        _userUpdater = userUpdater;
        _userDeleter = userDeleter;
        _syncSettingsOptions = syncSettingsOptions;
        _logger = logger;
    }

    public async Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids,
        CancellationToken cancellationToken = default)
    {
        var memberMap = new Dictionary<DirectoryGuid, MemberModel>();

        foreach (var groupId in trackingGroupGuids)
        {
            await ProcessGroupChanges(groupId, memberMap, cancellationToken);
        }

        var toCreate = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Create)
            .ToList();
        var toUpdate = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Update)
            .ToList();
        var toDelete = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Delete)
            .Select(m => m.Identity)
            .ToList();
        
        await _userCreator.CreateManyAsync(toCreate, cancellationToken);
        await _userUpdater.UpdateManyAsync(toUpdate, cancellationToken);
        await _userDeleter.DeleteManyAsync(toDelete, cancellationToken);
    }
    
    private async Task ProcessGroupChanges(DirectoryGuid groupId,
        Dictionary<DirectoryGuid, MemberModel> memberMap,
        CancellationToken cancellationToken)
    {
        var referenceGroup = await _groupPort.GetByGuidAsync(groupId);
        if (referenceGroup is null)
        {
            return;
        }

        var cachedGroup = _groupDatabase.FindById(groupId);

        if (cachedGroup is null)
        {
            cachedGroup = GroupModel.Create(referenceGroup.Id, []);
            _groupDatabase.Insert(cachedGroup);
        }

        if (cachedGroup.MembersHash == referenceGroup.MembersHash)
        {
            return;
        }

        var removedIds = cachedGroup.MemberIds.Except(referenceGroup.MemberIds).ToArray();
        var addedIds = referenceGroup.MemberIds.Except(cachedGroup.MemberIds).ToArray();
        
        HandleRemovedMembers(groupId, removedIds, memberMap);
        await HandleAddedMembers(groupId, addedIds, memberMap, cancellationToken);

        SetMembersGroupMapping(memberMap.Values);
    }
    
    private void HandleRemovedMembers(DirectoryGuid groupId,
        IEnumerable<DirectoryGuid> removedIds,
        Dictionary<DirectoryGuid, MemberModel> memberMap)
    {
        var members = FindOrLoadMembers(removedIds, memberMap);

        foreach (var member in members)
        {
            member.RemoveGroups([groupId]);

            if (member.GroupIds.Count == 0)
            {
                member.MarkForDelete();
            }
            else if (member.Operation != ChangeOperation.Create)
            {
                member.MarkForUpdate();
            }
        }
    }
    
    private async Task HandleAddedMembers(DirectoryGuid groupId,
        IEnumerable<DirectoryGuid> addedIds,
        Dictionary<DirectoryGuid, MemberModel> memberMap,
        CancellationToken cancellationToken)
    {
        var existingMembers = FindOrLoadMembers(addedIds, memberMap);
        var existingIds = existingMembers.Select(m => m.Id).ToHashSet();
        var newIds = addedIds.Except(existingIds).ToArray();

        var requiredNames = _syncSettingsOptions.GetRequiredAttributeNames();
        
        var newMembers = await _memberPort.GetByGuidsAsync(newIds, requiredNames, cancellationToken);
        foreach (var entry in newMembers)
        {
            var member = MemberModel.Create(entry.Id, entry.Identity, entry.Attributes, []);
            member.AddGroups([groupId]);
            member.MarkForCreate();
            memberMap[entry.Id] = member;
        }

        foreach (var member in existingMembers)
        {
            member.AddGroups([groupId]);
            if (member.Operation != ChangeOperation.Create)
            {
                member.MarkForUpdate();
            }
        }
    }

    private void SetMembersGroupMapping(IEnumerable<MemberModel> members)
    {
        var syncSettings = _syncSettingsOptions.Current;
        var groupMappingMap = syncSettings.DirectoryGroupMappings
            .ToDictionary(
                kpv => new DirectoryGuid(Guid.Parse(kpv.DirectoryGroup)),
                kpv => kpv.SignUpGroups.ToArray()
            );
        
        foreach (var member in members)
        {
            _userGroupsMapper.SetUserCloudGroupsChanges(member, groupMappingMap);
        }
    }
    
    private List<MemberModel> FindOrLoadMembers(IEnumerable<DirectoryGuid> ids,
        Dictionary<DirectoryGuid, MemberModel> memberMap)
    {
        var result = new List<MemberModel>();

        var notFoundInMap = new List<DirectoryGuid>();

        foreach (var id in ids)
        {
            if (memberMap.TryGetValue(id, out var cached))
            {
                result.Add(cached);
            }
            else
            {
                notFoundInMap.Add(id);
            }
        }

        if (notFoundInMap.Count > 0)
        {
            var loaded = _memberDatabase.FindById(notFoundInMap);
            foreach (var m in loaded)
            {
                memberMap[m.Id] = m;
                result.Add(m);
            }
        }

        return result;
    }
}
