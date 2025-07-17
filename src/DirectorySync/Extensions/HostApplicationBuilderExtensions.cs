using DirectorySync.Services;

namespace DirectorySync.Extensions;

internal static class HostApplicationBuilderExtensions
{
    public static void AddHostedServices(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Services.AddSingleton<OrderBoard>();
        builder.Services.AddHostedService<WorkloadDispatcher>();
    }
}
