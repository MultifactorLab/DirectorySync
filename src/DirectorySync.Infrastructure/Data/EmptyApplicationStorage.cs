using DirectorySync.Application.Models.Entities;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports;

namespace DirectorySync.Infrastructure.Data;

internal class EmptyApplicationStorage : IApplicationStorage
{
    public IEnumerable<CachedDirectoryGroup> FindGroups(IEnumerable<DirectoryGuid> ids) => default;
    public CachedDirectoryGroup? FindGroup(DirectoryGuid id) => default;
    public void InsertGroup(CachedDirectoryGroup group) { }
    public void UpdateGroup(CachedDirectoryGroup group) { }

    public bool IsGroupCollectionExists() => false;
}
