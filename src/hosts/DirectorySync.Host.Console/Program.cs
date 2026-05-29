using DirectorySync.Extensions;
using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Logging;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using Microsoft.Extensions.Hosting;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    DirectorySyncHost.Configure(builder, args, DirectorySyncHostingOptions.ResolveForegroundHostEntry());

    var built = builder.Build();
    host = built;
    built.RegisterApplicationHostEventsLogging();
    built.Run();
}
catch (PullCloudConfigException ex)
{
    StartupLogger.Error(ex, "Failed to start DirectorySync: {Message}. Response: {Response}", ex.Message, ex.Response);
}
catch (Exception ex)
{
    StartupLogger.Error(ex, "Failed to start DirectorySync");
}
finally
{
    await (host?.StopAsync() ?? Task.CompletedTask);
}
