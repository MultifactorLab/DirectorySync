using DirectorySync.Application.Ports;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class AddLiteDbStorageExtension
{
    public static void AddLiteDbStorage(this HostApplicationBuilder builder, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var localAppData = GetLocalAppData();
        var dir = Path.Combine(localAppData, "Multifactor", "Directory Sync");
        
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        builder.Services.Configure<LiteDbConfig>(x =>
        {
            var path = Path.Combine(dir, "storage.db");
            x.ConnectionString = $"Filename={path};Upgrade=true";

            StartupLogger.Information("Database location: {Location:l}", path);
        });
        builder.Services.AddSingleton<LiteDbConnection>();
        builder.Services.AddSingleton((Func<IServiceProvider, ILiteDbConnection>)(prov =>
        {
            var conn = prov.GetRequiredService<LiteDbConnection>();

            if (DatabaseCleanupRequested(args))
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

    private static string GetLocalAppData()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Environment.ExpandEnvironmentVariables("%localappdata%");
        }

        throw new PlatformNotSupportedException("Only Windows platform");
    }

    private static bool DatabaseCleanupRequested(params string[] args)
    {
        const string cleanupToken = "cleanup";
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
