using System.Text.RegularExpressions;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Repositories
{
    public interface IGroupRepository
    {
        IEnumerable<Group> FindById(IEnumerable<DirectoryGuid> ids);
        Group? FindById(DirectoryGuid id);
        void Insert(Group group);
        void Update(Group group);
        void Delete(Group group);
    }
}
