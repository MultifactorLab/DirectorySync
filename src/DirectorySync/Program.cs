using System.Runtime.InteropServices;
using DirectorySync.Application;
using DirectorySync.ConfigSources;
using DirectorySync.Exceptions;
using DirectorySync.Extensions;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        throw new PlatformNotSupportedException("For Windows platform only");
    }

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = Literals.ServiceName;
    });

    builder.Configuration.AddEnvironmentVariables("DIRECTORYSYNC_");
    if (builder.Environment.EnvironmentName == "localhost")
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    builder.RegisterLogger();

    builder.Configuration.AddMultifactorCloudConfiguration();

    builder.AddApplicationServices();
    builder.AddInfrastructureServices();
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
