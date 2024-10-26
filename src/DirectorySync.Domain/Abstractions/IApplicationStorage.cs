using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Domain.Abstractions;

public interface IApplicationStorage
{
    CachedDirectoryGroup? FindGroup(DirectoryGuid id);
    void CreateGroup(CachedDirectoryGroup group);
    void UpdateGroup(CachedDirectoryGroup group);
}
