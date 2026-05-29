using DirectorySync.Application.Ports.Databases;
using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Adapters.LiteDb;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Configurations;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Extensions;

internal static class DatabaseAdapterBuilderExtensions
{
    public static void AddLiteDbAdapter(
        this HostApplicationBuilder builder,
        DirectorySyncRuntimeMode runtimeMode,
        params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        builder.Services.AddOptions<StorageOptions>()
            .BindConfiguration("Storage")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<LiteDbConfig>()
            .PostConfigure<IOptions<StorageOptions>>((liteDb, storageAccessor) =>
            {
                var path = ResolveLiteDbPath(storageAccessor.Value, runtimeMode);
                liteDb.ConnectionString = $"Filename={path};Upgrade=true";
                StartupLogger.Information("Database location: {Location:l}", path);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

    private static string ResolveLiteDbPath(StorageOptions storage, DirectorySyncRuntimeMode runtimeMode)
    {
        var fileName = string.IsNullOrWhiteSpace(storage.LiteDbFileName)
            ? "storage.db"
            : storage.LiteDbFileName.Trim();

        var baseDir = ApplicationPathResolver.ResolveStorageBaseDirectory(storage, runtimeMode);
        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        return Path.Combine(baseDir, fileName);
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
