using LiteDB;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Data;

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
