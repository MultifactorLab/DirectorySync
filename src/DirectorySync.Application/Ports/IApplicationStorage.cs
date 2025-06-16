using DirectorySync.Application.Models.Entities;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports;

public interface IApplicationStorage
{
    bool IsGroupCollectionExists();
    IEnumerable<CachedDirectoryGroup> FindGroups(IEnumerable<DirectoryGuid> ids);
    CachedDirectoryGroup? FindGroup(DirectoryGuid id);
    void InsertGroup(CachedDirectoryGroup group);
    void UpdateGroup(CachedDirectoryGroup group);
}
