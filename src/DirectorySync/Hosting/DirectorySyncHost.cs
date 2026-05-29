using System.Runtime.InteropServices;
using DirectorySync.Application;
using DirectorySync.Configuration;
using DirectorySync.Extensions;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace DirectorySync.Hosting;

/// <summary>
/// Shared Directory Sync generic-host composition. Platform entrypoints call
/// <see cref="Configure"/> then build/run the host; deployment installers only ship binaries and config.
/// </summary>
public static class DirectorySyncHost
{
    /// <summary>
    /// Registers configuration, logging, application and infrastructure services for the worker.
    /// Does not build or run the host.
    /// </summary>
    /// <param name="builder">Application host builder.</param>
    /// <param name="args">Process command-line arguments.</param>
    /// <param name="options">Adapter-chosen hosting mode and Windows-specific configuration flags.</param>
    public static void Configure(HostApplicationBuilder builder, string[]? args, DirectorySyncHostingOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        args ??= [];

        ApplyWindowsServiceAdapter(builder, options);
        builder.Services.Configure<HostOptions>(builder.Configuration.GetSection("Host"));
        ApplyConfigurationSources(builder, args, options);

        DirectorySyncPathBootstrap.Initialize(options.RuntimeMode);
        builder.RegisterLogger(options.RuntimeMode, args);
        builder.Configuration.AddCloudConfigurationSource();

        builder.AddApplicationServices();
        builder.AddInfrastructureServices(options.RuntimeMode, args);
        builder.AddHostedServices();
    }

    private static void ApplyWindowsServiceAdapter(HostApplicationBuilder builder, DirectorySyncHostingOptions options)
    {
        if (options.RuntimeMode != DirectorySyncRuntimeMode.WindowsService)
        {
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException($"{DirectorySyncRuntimeMode.WindowsService} mode is only supported on Windows.");
        }

        builder.Services.AddWindowsService(o => { o.ServiceName = Literals.ServiceName; });
    }

    /// <summary>
    /// Configuration precedence (later wins for the same key), aligned with deployment adapters:
    /// <list type="number">
    /// <item><description>Host defaults: <c>appsettings.json</c>, <c>appsettings.{Environment}.json</c>, generic environment variables, command line.</description></item>
    /// <item><description>Windows MSI only: machine-level <c>DIRECTORYSYNC_*</c> via <see cref="ConfigurationBuilderExtensions.AddSystemEnvironmentVariablesSource"/>.</description></item>
    /// <item><description>Process non-empty <c>DIRECTORYSYNC_*</c> (Linux systemd env, Docker env, interactive shells).</description></item>
    /// <item><description>Optional <c>Host</c> section (e.g. <c>ShutdownTimeout</c>) bound to <see cref="Microsoft.Extensions.Hosting.HostOptions"/>.</description></item>
    /// <item><description>Debug localhost user secrets when enabled.</description></item>
    /// </list>
    /// Cloud sync settings are registered afterward in <see cref="Configure"/> and override earlier sources where the cloud API supplies values.
    /// </summary>
    private static void ApplyConfigurationSources(
        HostApplicationBuilder builder,
        string[] args,
        DirectorySyncHostingOptions options)
    {
        if (options.IncludeWindowsMachineEnvironmentConfiguration)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("Machine-level environment configuration is only supported on Windows.");
            }

            builder.Configuration.AddSystemEnvironmentVariablesSource("DIRECTORYSYNC_");
        }

        builder.Configuration.AddEnvironmentVariablesNonEmpty("DIRECTORYSYNC_");

#if DEBUG
        if (builder.Environment.EnvironmentName == "localhost")
        {
            builder.Configuration.AddUserSecrets(typeof(DirectorySyncHost).Assembly);
        }
#endif
    }
}
