using LiteDB;

namespace DirectorySync.Infrastructure.Data;

public interface ILiteDbConnection
{
    LiteDatabase Database { get; }
}
