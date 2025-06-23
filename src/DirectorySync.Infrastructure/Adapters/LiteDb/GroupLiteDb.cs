using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class GroupLiteDb : IGroupDatabase
{
    public ReadOnlyCollection<GroupModel> FindById(IEnumerable<DirectoryGuid> ids)
    {
        throw new NotImplementedException();
    }

    public GroupModel? FindById(DirectoryGuid id)
    {
        throw new NotImplementedException();
    }

    public void Insert(GroupModel group)
    {
        throw new NotImplementedException();
    }

    public void UpdateMany(IEnumerable<GroupModel> groups)
    {
        throw new NotImplementedException();
    }
}
