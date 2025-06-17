using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Repositories
{
    public interface IMemberRepository
    {
        ReadOnlyCollection<MemberModel> FindAll();
        ReadOnlyCollection<MemberModel> FindById(IEnumerable<DirectoryGuid> ids);
        MemberModel? FindById(DirectoryGuid id);
        void Insert(MemberModel group);
        void Update(MemberModel group);
        void Delete(MemberModel group);
    }
}
