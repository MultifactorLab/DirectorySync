using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Data.Extensions;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data;

internal class LiteDbApplicationStorage : IApplicationStorage
{
    private readonly ILiteDbConnection _connection;

    public LiteDbApplicationStorage(ILiteDbConnection connection)
    {
        _connection = connection;
    }
    
    public CachedDirectoryGroup? FindGroup(DirectoryGuid guid)
    {
        ArgumentNullException.ThrowIfNull(guid);
        
        var collection = _connection.Database.GetCollection<DirectoryGroupPersistenceModel>();

        var group = collection.FindOne(x => x.Id == guid.Value);
        if (group is null)
        {
            return null;
        }

        var directoryGroup = group.ToDomainModel();
        return directoryGroup;
    }

    public void InsertGroup(CachedDirectoryGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var collection = _connection.Database.GetCollection<DirectoryGroupPersistenceModel>();
        if (collection.FindById(group.Guid.Value) is not null)
        {
            throw new Exception($"Group '{group}' already exists");
        }

        var model = group.ToPersistenceModel();
        collection.Insert(model);
    }

    public void UpdateGroup(CachedDirectoryGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        
        var collection = _connection.Database.GetCollection<DirectoryGroupPersistenceModel>();
        var existed = collection.FindById(group.Guid.Value);
        if (existed is null)
        {
            throw new Exception($"Group '{group}' not found");
        }

        var model = group.ToPersistenceModel();
        collection.Update(model);
    }
}
