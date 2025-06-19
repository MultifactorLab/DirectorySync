using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Services;

public interface IUserGroupsMapper
{
    void SetUserCloudGroupsChanges(MemberModel member, Dictionary<DirectoryGuid, string[]> groupMappingOptions);
}

public class UserGroupsMapper
{
        
}
