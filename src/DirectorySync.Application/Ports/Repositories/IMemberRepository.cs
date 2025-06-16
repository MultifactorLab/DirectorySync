using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Repositories
{
    public interface IMemberRepository
    {
        IEnumerable<MemberModel> FindById(IEnumerable<DirectoryGuid> ids);
        GroupModel? FindById(DirectoryGuid id);
        void Insert(MemberModel group);
        void Update(MemberModel group);
        void Delete(MemberModel group);
    }
}
