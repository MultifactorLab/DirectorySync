using DirectorySync.Services;

namespace DirectorySync.Extensions;

internal static class AddHostedServicesExtension
{
    public static void AddHostedServices(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Services.AddSingleton<WorkloadsTasks>();
        builder.Services.AddHostedService<UserSynchronizer>();
        builder.Services.AddHostedService<NewUserHandler>();
        builder.Services.AddOptions<SyncOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
    }
}
