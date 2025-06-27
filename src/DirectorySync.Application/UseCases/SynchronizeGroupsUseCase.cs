using System.Collections.ObjectModel;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
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
    private readonly IGroupUpdater _groupUpdater;
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeGroupsUseCase> _logger;

    public SynchronizeGroupsUseCase(IGroupDatabase groupDatabase,
        IMemberDatabase memberDatabase,
        ILdapGroupPort groupPort,
        ILdapMemberPort memberPort,
        IUserGroupsMapper userGroupsMapper,
        IUserCreator userCreator,
        IUserUpdater userUpdater,
        IUserDeleter userDeleter,
        IGroupUpdater groupUpdater,
        ISyncSettingsOptions syncSettingsOptions,
        CodeTimer codeTimer,
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
        _groupUpdater = groupUpdater;
        _syncSettingsOptions = syncSettingsOptions;
        _codeTimer = codeTimer;
        _logger = logger;
    }

    public async Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(ApplicationEvent.StartUserScanning, "Start synchronization for groups: {group}", trackingGroupGuids);
        
        var memberMap = new Dictionary<DirectoryGuid, MemberModel>();

        foreach (var groupId in trackingGroupGuids)
        {
            using var withGroup = _logger.EnrichWithGroup(groupId);
            ProcessGroupChanges(groupId, memberMap, cancellationToken);
        }

        var toCreate = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Create)
            .ToList();
        var toUpdate = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Update)
            .ToList();
        var toDelete = memberMap.Values
            .Where(m => m.Operation == ChangeOperation.Delete)
            .ToList();

        ReadOnlyCollection<MemberModel> created = ReadOnlyCollection<MemberModel>.Empty;
        ReadOnlyCollection<MemberModel> updated = ReadOnlyCollection<MemberModel>.Empty;
        ReadOnlyCollection<MemberModel> deleted = ReadOnlyCollection<MemberModel>.Empty;
        
        if (toCreate.Count != 0)
        {
            created = await _userCreator.CreateManyAsync(toCreate, cancellationToken);
        }

        if (toUpdate.Count != 0)
        {
            updated = await _userUpdater.UpdateManyAsync(toUpdate, cancellationToken);
        }

        if (toDelete.Count != 0)
        {
            deleted = await _userDeleter.DeleteManyAsync(toDelete, cancellationToken);
        }
        
        _groupUpdater.UpdateGroupsWithMembers(created.Concat(updated).Concat(deleted));
        
        _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete groups synchronization");
    }
    
    private void ProcessGroupChanges(DirectoryGuid groupId,
        Dictionary<DirectoryGuid, MemberModel> memberMap,
        CancellationToken cancellationToken)
    {
        var getGroupTimer = _codeTimer.Start("Get Reference Group");
        var referenceGroup = _groupPort.GetByGuidAsync(groupId);
        getGroupTimer.Stop();
        if (referenceGroup is null)
        {
            _logger.LogWarning("Reference group not found: {Group:l}", groupId);
            return;
        }
        _logger.LogDebug("Reference group found: {Group:l}", referenceGroup);

        var cachedGroup = _groupDatabase.FindById(groupId);

        if (cachedGroup is null)
        {
            _logger.LogDebug("Reference group {Group} is not cached and now it will", groupId);
            cachedGroup = GroupModel.Create(referenceGroup.Id, []);
            _groupDatabase.Insert(cachedGroup);
        }

        if (cachedGroup.MembersHash == referenceGroup.MembersHash)
        {
            _logger.LogDebug("Group {Group} has no changes", groupId);
            return;
        }

        var removedIds = cachedGroup.MemberIds.Except(referenceGroup.MemberIds).ToArray();
        if (removedIds.Length == 0)
        {
            _logger.LogDebug("Removed users not found...");
        }
        else
        {
            HandleRemovedMembers(groupId, removedIds, memberMap);   
        }
        
        var addedIds = referenceGroup.MemberIds.Except(cachedGroup.MemberIds).ToArray();
        if (addedIds.Length == 0)
        {
            _logger.LogDebug("Added users not found...");
        }
        else
        {
            HandleAddedMembers(groupId, addedIds, memberMap, cancellationToken);
        }

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
    
    private void HandleAddedMembers(DirectoryGuid groupId,
        IEnumerable<DirectoryGuid> addedIds,
        Dictionary<DirectoryGuid, MemberModel> memberMap,
        CancellationToken cancellationToken)
    {
        var existingMembers = FindOrLoadMembers(addedIds, memberMap);
        var existingIds = existingMembers.Select(m => m.Id).ToHashSet();
        var newIds = addedIds.Except(existingIds).ToArray();

        var requiredNames = _syncSettingsOptions.GetRequiredAttributeNames();
        
        var newMembers = _memberPort.GetByGuids(newIds, requiredNames, cancellationToken);
        foreach (var member in newMembers)
        {
            member.AddGroups([groupId]);
            member.MarkForCreate();
            memberMap[member.Id] = member;
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
            var(toAdd, toRemove) = _userGroupsMapper.GetCloudGroupChanges(member, groupMappingMap);
            
            member.AddCloudGroups(toAdd);
            member.RemoveCloudGroups(toRemove);
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
            var loaded = _memberDatabase.FindManyById(notFoundInMap);
            foreach (var m in loaded)
            {
                memberMap[m.Id] = m;
                result.Add(m);
            }
        }

        return result;
    }
}
