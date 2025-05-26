using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports;

public interface IApplicationStorage
{
    IEnumerable<CachedDirectoryGroup> GetAllGroups();
    CachedDirectoryGroup? FindGroup(DirectoryGuid id);
    void InsertGroup(CachedDirectoryGroup group);
    void UpdateGroup(CachedDirectoryGroup group);
}
