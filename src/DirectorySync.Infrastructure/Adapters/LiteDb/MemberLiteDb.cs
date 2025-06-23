using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Dto.LiteDb;
using LiteDB;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class MemberLiteDb : IMemberDatabase
{
    private readonly ILiteCollection<MemberPersistenceModel> _collection;

    public MemberLiteDb(ILiteDbConnection connection)
    {
        _collection = connection.Database.GetCollection<MemberPersistenceModel>();
    }
    
    public ReadOnlyCollection<MemberModel> FindAll()
    {
        var members = _collection.FindAll();

        if (members is null)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }
        
        return members
            .Select(MemberPersistenceModel.ToDomainModel)
            .ToArray()
            .AsReadOnly();
    }

    public ReadOnlyCollection<MemberModel> FindManyById(IEnumerable<DirectoryGuid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        var idSet = ids.Select(x => new BsonValue(x.Value)).ToArray();
        if (idSet.Length == 0)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }

        var dbMembers = _collection.Query()
            .Where(Query.In(nameof(MemberPersistenceModel.Id), idSet))
            .ToList();

        if (dbMembers is null)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }
        
        var members = dbMembers.Select(MemberPersistenceModel.ToDomainModel);
        return members.ToArray().AsReadOnly();
    }

    public void InsertMany(IEnumerable<MemberModel> members)
    {
        ArgumentNullException.ThrowIfNull(members, nameof(members));
        
        var dbModels = members.Select(MemberPersistenceModel.FromDomainModel);
        
        _collection.Insert(dbModels);
    }

    public void UpdateMany(IEnumerable<MemberModel> members)
    {
        ArgumentNullException.ThrowIfNull(members, nameof(members));

        var dbModels = members.Select(MemberPersistenceModel.FromDomainModel);
        
        _collection.Update(dbModels);
    }

    public void DeleteMany(IEnumerable<DirectoryGuid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));
        
        var idSet = ids.Select(x => new BsonValue(x.Value)).ToArray();
        
        _collection.DeleteMany(Query.In(nameof(MemberPersistenceModel.Id), idSet));
    }
}
