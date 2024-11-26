using DirectorySync.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class AddLiteDbStorageExtension
{
    public static void AddLiteDbStorage(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "DirectorySync");
        
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        builder.Services.Configure<LiteDbConfig>(x =>
        {

            var path = Path.Combine(dir, "storage.db");
            x.ConnectionString = $"Filename={path};Upgrade=true";
        });
        builder.Services.AddSingleton<LiteDbConnection>();
        builder.Services.AddSingleton((Func<IServiceProvider, ILiteDbConnection>)(prov =>
        {
            var conn = prov.GetRequiredService<LiteDbConnection>();

            if (DatabaseCleanupRequested())
            {
                var factory = prov.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger("DirectorySync");
                logger.LogWarning("Service cleanup requested: all cached data will be dropped");
                DropAllCollections(conn);
                logger.LogWarning("All cached data dropped");
            }

            return conn;
        }));
        builder.Services.AddTransient<IApplicationStorage, LiteDbApplicationStorage>();
    }

    private static bool DatabaseCleanupRequested()
    {
        const string cleanupToken = "--cleanup";
        var args = Environment.GetCommandLineArgs();
        return args.Contains(cleanupToken, StringComparer.OrdinalIgnoreCase);
    }

    private static void DropAllCollections(LiteDbConnection conn)
    {
        foreach (var coll in conn.Database.GetCollectionNames())
        {
            conn.Database.DropCollection(coll);
        }
    }
}
