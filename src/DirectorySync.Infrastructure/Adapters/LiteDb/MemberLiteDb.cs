using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class MemberLiteDb : IMemberDatabase
{
    public ReadOnlyCollection<MemberModel> FindAll()
    {
        throw new NotImplementedException();
    }

    public ReadOnlyCollection<MemberModel> FindManyById(IEnumerable<DirectoryGuid> ids)
    {
        throw new NotImplementedException();
    }

    public void InsertMany(IEnumerable<MemberModel> member)
    {
        throw new NotImplementedException();
    }

    public void UpdateMany(IEnumerable<MemberModel> members)
    {
        throw new NotImplementedException();
    }

    public void DeleteMany(IEnumerable<MemberModel> members)
    {
        throw new NotImplementedException();
    }
}
