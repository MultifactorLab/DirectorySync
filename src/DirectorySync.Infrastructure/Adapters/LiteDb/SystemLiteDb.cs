using DirectorySync.Application.Ports.Databases;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class SystemLiteDb : ISystemDatabase
{
    public bool IsDatabaseInitialized()
    {
        throw new NotImplementedException();
    }
}
