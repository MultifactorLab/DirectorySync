using DirectorySync.Application;
using DirectorySync.Infrastructure.Logging;

namespace DirectorySync.Extensions
{
    internal static class RegisterApplicationHostEventsLoggingExtension
    {
        public static void RegisterApplicationHostEventsLogging(this IHost host)
        {
            ArgumentNullException.ThrowIfNull(host);

            var prov = host.Services;
            var events = prov.GetRequiredService<IHostApplicationLifetime>();

            events.ApplicationStarted.Register(() =>
            {
                var factory = prov.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger("DirectorySync"); 
                logger.LogInformation(ApplicationEvent.ApplicationStarted, "Application successfully started");
            });

            events.ApplicationStopped.Register(() =>
            {
                var factory = prov.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger("DirectorySync");
                logger.LogInformation(ApplicationEvent.ApplicationStopped, "Application successfully stopped");
            });
        }
    }
}
