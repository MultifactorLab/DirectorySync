using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Databases;

public interface IGroupDatabase
{
    ReadOnlyCollection<GroupModel> FindById(IEnumerable<DirectoryGuid> ids);
    GroupModel? FindById(DirectoryGuid id);
    void Insert(GroupModel group);
    void UpdateMany(IEnumerable<GroupModel> groups);
    void DeleteMany(IEnumerable<DirectoryGuid> ids);
}
