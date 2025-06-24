using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Infrastructure.Adapters.Multifactor;
using DirectorySync.Infrastructure.Configurations;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Shared.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;

namespace DirectorySync.Infrastructure.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static void AddSyncOptions(this HostApplicationBuilder builder)
    {
        builder.Services.AddOptions<SyncSettings>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
    }
    
}
