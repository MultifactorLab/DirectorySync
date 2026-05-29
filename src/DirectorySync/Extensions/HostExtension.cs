using DirectorySync.Application;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Extensions
{
    public static class HostExtension
    {
        public static void RegisterApplicationHostEventsLogging(this IHost host)
        {
            ArgumentNullException.ThrowIfNull(host);

            var prov = host.Services;
            var events = prov.GetRequiredService<IHostApplicationLifetime>();
            var factory = prov.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("DirectorySync");

            events.ApplicationStarted.Register(() =>
            {
                logger.LogInformation(ApplicationEvent.ApplicationStarted,
                    "Copyright Multifactor 2019-{0}, ver.: {1}. Application successfully started.",
                    DateTime.Now.Year,
                    Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "1.0.1");
            });

            events.ApplicationStopping.Register(() =>
            {
                logger.LogInformation(ApplicationEvent.ApplicationShutdownRequested,
                    "Shutdown requested; generic host is stopping (maps to Windows SCM stop, SIGTERM/SIGINT, systemd stop, or docker stop).");
            });

            events.ApplicationStopped.Register(() =>
            {
                logger.LogInformation(ApplicationEvent.ApplicationStopped, "Application successfully stopped");
            });
        }
    }
}
