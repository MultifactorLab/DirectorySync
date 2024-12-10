using DirectorySync.Application;

namespace DirectorySync.Extensions
{
    internal static class HostExtension
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
                logger.LogInformation(ApplicationEvent.ApplicationStarted, "Application successfully started");
            });

            events.ApplicationStopped.Register(() =>
            {
                logger.LogInformation(ApplicationEvent.ApplicationStopped, "Application successfully stopped");
            });
        }
    }
}
