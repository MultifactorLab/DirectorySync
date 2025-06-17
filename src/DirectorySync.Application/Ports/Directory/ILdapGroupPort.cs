using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory
{
    public interface ILdapGroupPort
    {
        Task<GroupModel?> GetByGuidAsync(DirectoryGuid objectGuid);
        Task<ReadOnlyCollection<GroupModel>?> GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids);
        Task<ReadOnlyCollection<DirectoryGuid>> GetGroupMemberIdsRecursiveAsync(DirectoryGuid groupGuid);
        Task<ReadOnlyCollection<GroupModel>> GetChangedAfterAsync(long usnChanged);
    }
}
