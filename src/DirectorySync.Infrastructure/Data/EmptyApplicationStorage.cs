using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Infrastructure.Data;

internal class EmptyApplicationStorage : IApplicationStorage
{
    public CachedDirectoryGroup? FindGroup(DirectoryGuid id) => default;
    public void InsertGroup(CachedDirectoryGroup group) { }
    public void UpdateGroup(CachedDirectoryGroup group) { }
}
