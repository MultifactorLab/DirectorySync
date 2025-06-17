using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Repositories
{
    public interface IGroupRepository
    {
        ReadOnlyCollection<GroupModel> FindById(IEnumerable<DirectoryGuid> ids);
        GroupModel? FindById(DirectoryGuid id);
        void Insert(GroupModel group);
        void Update(GroupModel group);
        void Delete(GroupModel group);
    }
}
