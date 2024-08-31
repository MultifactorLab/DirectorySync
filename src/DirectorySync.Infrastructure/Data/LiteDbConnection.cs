using LiteDB;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Data;

public interface ILiteDbConnection
{
    LiteDatabase Database { get; }
}

internal class LiteDbConnection : ILiteDbConnection, IDisposable
{
    public LiteDatabase Database { get; }
    
    public LiteDbConnection(IOptions<LiteDbConfig> options)
    {
        Database = new LiteDatabase(options.Value.ConnectionString);
    }
    
    public void Dispose()
    {
        Database.Dispose();
    }
}
