using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Infrastructure.Adapters.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static void AddSyncOptions(this HostApplicationBuilder builder)
    {
        builder.Services.AddOptions<SyncSettings>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();

        builder.Services.AddTransient<ISyncSettingsOptions, SyncSettingsOptions>();
    }
    
}
