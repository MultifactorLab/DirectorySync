using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Dto.LiteDb;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class SystemLiteDb : ISystemDatabase
{
    private readonly ILiteDbConnection _connection;

    public SystemLiteDb(ILiteDbConnection connection)
    {
        _connection = connection;
    }
    
    public bool IsDatabaseInitialized()
    {
        return _connection.Database.CollectionExists(nameof(GroupPersistenceModel));
    }
}
