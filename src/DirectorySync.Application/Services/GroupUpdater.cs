using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Services;

public interface IGroupUpdater
{
    void UpdateGroupsWithMembers(IEnumerable<MemberModel> members);
}

public class GroupUpdater: IGroupUpdater
{
    private readonly IGroupDatabase _groupDatabase;
    private readonly ILogger<GroupUpdater> _logger;

    public GroupUpdater(IGroupDatabase groupDatabase,
        ILogger<GroupUpdater> logger)
    {
        _groupDatabase = groupDatabase;
        _logger = logger;
    }
    
    public void UpdateGroupsWithMembers(IEnumerable<MemberModel> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        
        var groupsMap = new Dictionary<DirectoryGuid, GroupModel>();
        
        foreach (var member in members)
        {
            foreach (var groupId in member.AddedGroupIds)
            {
                var group = FindOrLoadGroup(groupId, groupsMap);
                if (group is not null)
                {
                    group.AddMembers([member.Id]);
                    group.MarkForUpdate();
                }
            }

            foreach (var groupId in member.RemovedGroupIds)
            {
                var group = FindOrLoadGroup(groupId, groupsMap);
                if (group is not null)
                {
                    group.RemoveMembers([member.Id]);
                    group.MarkForUpdate();
                }
            }
        }
        
        _groupDatabase.UpdateMany(groupsMap.Values.Where(g => g.Operation == ChangeOperation.Update));
    }
    
    private GroupModel? FindOrLoadGroup(DirectoryGuid groupId,
        Dictionary<DirectoryGuid, GroupModel> groupsMap)
    {
        if (groupsMap.TryGetValue(groupId, out var group))
        {
            return group;
        }
        else
        {
            var dbGroup = _groupDatabase.FindById(groupId);

            if (dbGroup is null)
            {
                _logger.LogWarning($"Group with id {groupId} not found");
                return null;
            }
            
            groupsMap.Add(groupId, dbGroup);
            return dbGroup;
        }
    }
}
