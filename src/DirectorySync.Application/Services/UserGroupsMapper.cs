using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Services;

public interface IUserGroupsMapper
{
    void SetUserCloudGroupsChanges(MemberModel member, Dictionary<DirectoryGuid, string[]> groupMappingOptions);
    HashSet<DirectoryGuid> GetChangedDirectoryGroups(GroupMapping[] oldMappings, GroupMapping[] newMappings);

}

public class UserGroupsMapper : IUserGroupsMapper
{
    public void SetUserCloudGroupsChanges(MemberModel member, Dictionary<DirectoryGuid, string[]> groupMappingOptions)
    {
        foreach (var groupId in member.AddedGroupIds)
        {
            if (groupMappingOptions.TryGetValue(groupId, out var cloudGroups))
            {
                member.AddCloudGroups(cloudGroups);
            }
        }

        if (member.RemovedCloudGroups.Count > 0)
        {
            var memberGroups = member.GroupIds.Concat(member.RemovedGroupIds);

            var cloudGroupsToRemove = new List<string>();

            foreach (var groupId in member.RemovedGroupIds)
            {
                if (groupMappingOptions.TryGetValue(groupId, out var cloudGroups))
                {
                    cloudGroupsToRemove.AddRange(cloudGroups);
                }
            }
            
            var userSignUpGroups = new List<string>();

            foreach (var group in memberGroups)
            {
                if (groupMappingOptions.TryGetValue(group, out var signUpGroups))
                {
                    userSignUpGroups.AddRange(signUpGroups);
                }
            }

            var remainingUserGroups = new List<string>(userSignUpGroups);

            foreach (var groupToRemove in member.RemovedGroupIds)
            {
                var index = remainingUserGroups.IndexOf(groupToRemove);
                if (index != -1)
                {
                    remainingUserGroups.RemoveAt(index);
                }
            }

            var removedCloudGroups = cloudGroupsToRemove
                .Where(group => !remainingUserGroups.Contains(group))
                .Distinct()
                .ToArray();

             member.RemoveCloudGroups(removedCloudGroups);
        }
    }
    
    public HashSet<DirectoryGuid> GetChangedDirectoryGroups(GroupMapping[] oldMappings, GroupMapping[] newMappings)
    {
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
