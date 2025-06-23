using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Services;

public interface IUserGroupsMapper
{
    (string[] ToAdd, string[] ToRemove) GetCloudGroupChanges(MemberModel member, Dictionary<DirectoryGuid, string[]> groupMappingOptions);
    (string[] ToAdd, string[] ToRemove) SetUserCloudGroupsDiff(MemberModel member, Dictionary<DirectoryGuid, string[]> oldMappings, Dictionary<DirectoryGuid, string[]> newMappings);
    HashSet<DirectoryGuid> GetChangedDirectoryGroups(GroupMapping[] oldMappings, GroupMapping[] newMappings);

}

public class UserGroupsMapper : IUserGroupsMapper
{
    public (string[] ToAdd, string[] ToRemove) GetCloudGroupChanges(MemberModel member, Dictionary<DirectoryGuid, string[]> groupMappingOptions)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(groupMappingOptions);
        
        var addedCloudGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var removedCloudGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var groupId in member.AddedGroupIds)
        {
            if (groupMappingOptions.TryGetValue(groupId, out var cloudGroups))
            {
                addedCloudGroups.UnionWith(cloudGroups);
            }
        }

        if (member.RemovedCloudGroups.Count > 0)
        {
            foreach (var groupId in member.RemovedGroupIds)
            {
                if (groupMappingOptions.TryGetValue(groupId, out var cloudGroups))
                {
                    removedCloudGroups.UnionWith(cloudGroups);
                }
            }
        
            var stillMappedCloudGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var groupId in member.GroupIds)
            {
                if (groupMappingOptions.TryGetValue(groupId, out var cloudGroups))
                {
                    stillMappedCloudGroups.UnionWith(cloudGroups);
                }
            }
        
            removedCloudGroups.ExceptWith(stillMappedCloudGroups);
        }
        
        return (addedCloudGroups.ToArray(), removedCloudGroups.ToArray());
    }

    public (string[] ToAdd, string[] ToRemove) SetUserCloudGroupsDiff(MemberModel member,
        Dictionary<DirectoryGuid, string[]> oldMappings,
        Dictionary<DirectoryGuid, string[]> newMappings)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(oldMappings);
        ArgumentNullException.ThrowIfNull(newMappings);
        
        var allGroupIds = member.GroupIds
            .Concat(member.AddedGroupIds)
            .Concat(member.RemovedGroupIds)
            .Distinct();

        var oldCloudGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newCloudGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupId in allGroupIds)
        {
            if (oldMappings.TryGetValue(groupId, out var oldGroups))
            {
                oldCloudGroups.UnionWith(oldGroups);
            }

            if (newMappings.TryGetValue(groupId, out var newGroups))
            {
                newCloudGroups.UnionWith(newGroups);
            }
        }
        
        var toAdd = newCloudGroups.Except(oldCloudGroups).ToArray();
        var toRemove = oldCloudGroups.Except(newCloudGroups).ToArray();

        return (toAdd, toRemove);
    }

    public HashSet<DirectoryGuid> GetChangedDirectoryGroups(GroupMapping[] oldMappings, GroupMapping[] newMappings)
    {
        ArgumentNullException.ThrowIfNull(oldMappings);
        ArgumentNullException.ThrowIfNull(newMappings);
        
        var changedGroups = new HashSet<DirectoryGuid>();

        var oldMap = oldMappings.ToDictionary(
            m => m.DirectoryGroup,
            m => new HashSet<string>(m.SignUpGroups ?? [], StringComparer.OrdinalIgnoreCase));

        var newMap = newMappings.ToDictionary(
            m => m.DirectoryGroup,
            m => new HashSet<string>(m.SignUpGroups ?? [], StringComparer.OrdinalIgnoreCase));

        foreach (var key in oldMap.Keys.Union(newMap.Keys))
        {
            oldMap.TryGetValue(key, out var oldSet);
            newMap.TryGetValue(key, out var newSet);

            oldSet ??= [];
            newSet ??= [];

            if (!oldSet.SetEquals(newSet))
            {
                if (Guid.TryParse(key, out var guid))
                {
                    changedGroups.Add(new DirectoryGuid(guid));
                }
            }
        }

        return changedGroups;
    }
}
