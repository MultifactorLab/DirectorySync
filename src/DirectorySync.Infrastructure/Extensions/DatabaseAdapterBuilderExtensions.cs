using System.Runtime.InteropServices;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Extensions;

internal static class DatabaseAdapterBuilderExtensions
{
    public static void AddLiteDbAdapter(this HostApplicationBuilder builder, params string[] args)
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
        builder.Services.AddTransient<IMemberDatabase, MemberLiteDb>();
        builder.Services.AddTransient<IGroupDatabase, GroupLiteDb>();
        builder.Services.AddTransient<ISyncSettingsDatabase, SyncSettingsLiteDb>();
        builder.Services.AddTransient<ISystemDatabase, SystemLiteDb>();
        builder.Services.AddTransient<IDirectoryDomainDatabase, DirectoryDomainLiteDb>();
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
