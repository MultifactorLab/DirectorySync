using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports;

public interface IApplicationStorage
{
    IEnumerable<CachedDirectoryGroup> FindGroups(IEnumerable<DirectoryGuid> ids);
    CachedDirectoryGroup? FindGroup(DirectoryGuid id);
    void InsertGroup(CachedDirectoryGroup group);
    void UpdateGroup(CachedDirectoryGroup group);
}
