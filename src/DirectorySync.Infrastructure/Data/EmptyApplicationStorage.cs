using DirectorySync.Application.Ports;
using DirectorySync.Domain.Entities;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Infrastructure.Data;

internal class EmptyApplicationStorage : IApplicationStorage
{
    public CachedDirectoryGroup? FindGroup(DirectoryGuid id) => default;

    public IEnumerable<CachedDirectoryGroup> FindGroups(IEnumerable<DirectoryGuid> ids) => default;

    public void InsertGroup(CachedDirectoryGroup group) { }

    public bool IsGroupCollectionExists() => default;

    public void UpdateGroup(CachedDirectoryGroup group) { }
}
