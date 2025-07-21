using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Dto.LiteDb;
using LiteDB;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class DirectoryDomainLiteDb : IDirectoryDomainDatabase
{
    private readonly ILiteCollection<DirectoryDomainPersistenceModel> _collection;

    public DirectoryDomainLiteDb(ILiteDbConnection connection)
    {
        _collection = connection.Database.GetCollection<DirectoryDomainPersistenceModel>();
    }
    
    public ReadOnlyCollection<LdapDomain> FindAll()
    {
        var domains = _collection.FindAll();

        if (domains is null)
        {
            return ReadOnlyCollection<LdapDomain>.Empty;
        }
        
        return domains
            .Select(d => new LdapDomain(d.Domain))
            .ToArray()
            .AsReadOnly();
    }

    public void InsertMany(IEnumerable<LdapDomain> domains)
    {
        ArgumentNullException.ThrowIfNull(domains, nameof(domains));
        
        var dbModels = domains
            .Select(d => new DirectoryDomainPersistenceModel { Domain = d.Value });
        
        _collection.Insert(dbModels);
    }

    public void DeleteMany(IEnumerable<LdapDomain> domains)
    {
        ArgumentNullException.ThrowIfNull(domains, nameof(domains));
        
        var domainSet = domains.Select(x => new BsonValue(x.Value)).ToArray();
        
        _collection.DeleteMany(Query.In(nameof(DirectoryDomainPersistenceModel.Domain), domainSet));
    }
}
