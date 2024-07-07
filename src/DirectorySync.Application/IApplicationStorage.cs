using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application;

public interface IApplicationStorage
{
    CachedDirectoryGroup? FindGroup(DirectoryGuid id);
    void CreateGroup(CachedDirectoryGroup group);
    void UpdateGroup(CachedDirectoryGroup group);
}
