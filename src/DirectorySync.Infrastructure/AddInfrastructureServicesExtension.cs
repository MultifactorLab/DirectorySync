using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure;

public static class AddInfrastructureServicesExtension
{
    public static void AddInfrastructureServices(
        this HostApplicationBuilder builder,
        DirectorySyncRuntimeMode runtimeMode,
        params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddSyncOptions();
        builder.AddLdapAdapter();
        builder.AddLiteDbAdapter(runtimeMode, args);
        builder.AddMultifactorAdapter();
    }
}
