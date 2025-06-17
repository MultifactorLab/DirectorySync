using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Repositories;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.UseCases;

public interface ISynchronizeGroupsUseCase
{
    Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids, CancellationToken token = default);
}

public class SynchronizeGroupsUseCase : ISynchronizeGroupsUseCase
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILdapGroupPort _groupPort;
    private readonly ILdapMemberPort _memberPort;
    private readonly IUserCreator _userCreator;
    private readonly IUserUpdater _userUpdater;
    private readonly IUserDeleter _userDeleter;
    private readonly ILogger<SynchronizeGroupsUseCase> _logger;

    public SynchronizeGroupsUseCase(IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ILdapGroupPort groupPort,
        ILdapMemberPort memberPort,
        IUserCreator userCreator,
        IUserUpdater userUpdater,
        IUserDeleter userDeleter,
        ILogger<SynchronizeGroupsUseCase> logger)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _groupPort = groupPort;
        _memberPort = memberPort;
        _userCreator = userCreator;
        _userUpdater = userUpdater;
        _userDeleter = userDeleter;
        _logger = logger;
    }

    public async Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids,
        CancellationToken token = default)
    {
        var memberMap = new Dictionary<DirectoryGuid, MemberModel>();

        foreach (var groupId in trackingGroupGuids)
        {
            await ProcessGroupChanges(groupId, memberMap, token);
        }

        var toCreate = memberMap.Values.Where(m => m.Operation == ChangeOperation.Create).ToList();
        var toUpdate = memberMap.Values.Where(m => m.Operation == ChangeOperation.Update).ToList();
        var toDelete = memberMap.Values.Where(m => m.Operation == ChangeOperation.Delete).ToList();
        
        await _userCreator.CreateManyAsync(toCreate, token);
        await _userUpdater.UpdateManyAsync(toUpdate, token);
        await _userDeleter.DeleteManyAsync(toDelete, token);
    }
    
    private async Task ProcessGroupChanges(DirectoryGuid groupId,
        Dictionary<DirectoryGuid, MemberModel> memberMap,
        CancellationToken token)
    {
        var referenceGroup = await _groupPort.GetByGuidAsync(groupId);
        if (referenceGroup is null)
        {
            return;
        }

        var cachedGroup = _groupRepository.FindById(groupId);

        if (cachedGroup is null)
        {
            cachedGroup = GroupModel.Create(referenceGroup.Id, []);
            _groupRepository.Insert(cachedGroup);
        }

        if (cachedGroup.MembersHash == referenceGroup.MembersHash)
        {
            return;
        }

        var removedIds = cachedGroup.MemberIds.Except(referenceGroup.MemberIds).ToArray();
        var addedIds = referenceGroup.MemberIds.Except(cachedGroup.MemberIds).ToArray();

        HandleRemovedMembers(groupId, removedIds, memberMap);
        await HandleAddedMembers(groupId, addedIds, memberMap, token);
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
        CancellationToken token)
    {
        var existingMembers = FindOrLoadMembers(addedIds, memberMap);
        var existingIds = existingMembers.Select(m => m.Id).ToHashSet();
        var newIds = addedIds.Except(existingIds).ToArray();

        var newMembers = await _memberPort.GetByGuidsAsync(newIds);
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
            var loaded = _memberRepository.FindById(notFoundInMap);
            foreach (var m in loaded)
            {
                memberMap[m.Id] = m;
                result.Add(m);
            }
        }

        return result;
    }
}
