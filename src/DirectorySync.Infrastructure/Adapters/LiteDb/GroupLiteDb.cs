using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Dto.LiteDb;
using LiteDB;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class GroupLiteDb : IGroupDatabase
{
    private readonly ILiteCollection<GroupPersistenceModel> _collection;

    public GroupLiteDb(ILiteDbConnection connection)
    {
        _collection = connection.Database.GetCollection<GroupPersistenceModel>();
    }
    
    public ReadOnlyCollection<GroupModel> FindById(IEnumerable<DirectoryGuid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));
        
        var idSet = ids.Select(x => new BsonValue(x.Value)).ToArray();
        var dbGroups = _collection.Query()
            .Where(Query.In("_id", idSet))
            .ToList();

        if (dbGroups is null)
        {
            return ReadOnlyCollection<GroupModel>.Empty;
        }

        var groupModels = dbGroups.Select(GroupPersistenceModel.ToDomainModel);
        return groupModels.ToArray().AsReadOnly();
    }

    public GroupModel? FindById(DirectoryGuid id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        var group = _collection.FindById(id.Value);

        if (group is null)
        {
            return null;
        }
        
        return GroupPersistenceModel.ToDomainModel(group);
    }

    public void Insert(GroupModel group)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        
        if (_collection.FindById(group.Id.Value) is not null)
        {
            throw new Exception($"Group '{group}' already exists");
        }
        
        var dbModel = GroupPersistenceModel.FromDomainModel(group);
        
        _collection.Insert(dbModel);
    }

    public void UpdateMany(IEnumerable<GroupModel> groups)
    {
        ArgumentNullException.ThrowIfNull(groups, nameof(groups));

        var dbModels = groups.Select(GroupPersistenceModel.FromDomainModel);
        
        _collection.Update(dbModels);
    }

    public void DeleteMany(IEnumerable<DirectoryGuid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));
        
        var idSet = ids.Select(x => new BsonValue(x.Value)).ToArray();
        
        _collection.DeleteMany(Query.In("_id", idSet));
    }
}
