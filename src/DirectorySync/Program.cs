using System.Runtime.InteropServices;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
using DirectorySync.ConfigSources;
using DirectorySync.Exceptions;
using DirectorySync.Extensions;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.Data.Extensions;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        throw new PlatformNotSupportedException("Only for Windows platform");
    }

    builder.Configuration.AddEnvironmentVariables("DIRECTORYSYNC");
    if (builder.Environment.EnvironmentName == "localhost")
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = Literals.ServiceName;
    });

    builder.RegisterLogger();

    builder.Configuration.AddMultifactorCloudConfiguration();

    builder.AddApplicationServices();
    builder.AddLiteDbStorage();
    builder.AddHostedServices();
    builder.AddCodeTimer("Logging");
    

    host = builder.Build();
    host.RegisterApplicationHostEventsLogging();

    host.Run();
}
catch (PullMultifactorSettingsException ex)
{
    FallbackLogger.Error(ex, "Failed to start DirectorySync service: {Message}. Response: {Response}",
        ex.Message,
        ex.Response);
}
catch (Exception ex)
{
    FallbackLogger.Error(ex, "Failed to start DirectorySync service");
}
finally
{
    await (host?.StopAsync() ?? Task.CompletedTask);
}
