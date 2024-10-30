using DirectorySync.Services;

namespace DirectorySync.Extensions;

internal static class AddHostedServicesExtension
{
    public static void AddHostedServices(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Services.AddSingleton<OrderBoard>();
        builder.Services.AddHostedService<WorkloadDispatcher>();
        builder.Services.AddOptions<SyncOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
    }
}
