using System.Runtime.InteropServices;
using DirectorySync.Extensions;
using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Logging;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using Microsoft.Extensions.Hosting;

IHost? host = null;

try
{
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        throw new PlatformNotSupportedException("This executable is the Windows Service entrypoint. Use DirectorySync.Host.Console for Linux or Docker.");
    }

    var builder = Host.CreateApplicationBuilder(args);
    DirectorySyncHost.Configure(builder, args, DirectorySyncHostingOptions.WindowsServiceEntry);

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
