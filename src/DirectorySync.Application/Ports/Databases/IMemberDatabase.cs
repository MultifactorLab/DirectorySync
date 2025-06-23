using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Databases;

public interface IMemberDatabase
{
    ReadOnlyCollection<MemberModel> FindAll();
    ReadOnlyCollection<MemberModel> FindManyById(IEnumerable<DirectoryGuid> ids);
    void InsertMany(IEnumerable<MemberModel> member);
    void UpdateMany(IEnumerable<MemberModel> members);
    void DeleteMany(IEnumerable<MemberModel> members);
}
