using System.Runtime.InteropServices;
using DirectorySync.Application;
using DirectorySync.ConfigSources.SystemEnvironmentVariables;
using DirectorySync.Extensions;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.ConfigurationSources.MultifactorCloud;
using DirectorySync.Infrastructure.Logging;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    args ??= [];

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        throw new PlatformNotSupportedException("For Windows platform only");
    }

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = Literals.ServiceName;
    });


    builder.Configuration.AddSystemEnvironmentVariables("DIRECTORYSYNC_");
    builder.Configuration.AddEnvironmentVariables("DIRECTORYSYNC_");

#if DEBUG
    if (builder.Environment.EnvironmentName == "localhost")
    {
        builder.Configuration.AddUserSecrets<Program>();
    }
#endif

    builder.RegisterLogger(args);

    builder.Configuration.AddMultifactorCloudConfiguration();

    var configurationRoot = (IConfigurationRoot)builder.Configuration;
    builder.Services.AddSingleton(configurationRoot);

    builder.AddApplicationServices();
    builder.AddInfrastructureServices(args);
    builder.AddHostedServices();

    host = builder.Build();
    host.RegisterApplicationHostEventsLogging();

    host.Run();
}
catch (PullCloudConfigException ex)
{
    StartupLogger.Error(ex, "Failed to start DirectorySync service: {Message}. Response: {Response}", ex.Message, ex.Response);
}
catch (Exception ex)
{
    StartupLogger.Error(ex, "Failed to start DirectorySync service");
}
finally
{
    await (host?.StopAsync() ?? Task.CompletedTask);
}
