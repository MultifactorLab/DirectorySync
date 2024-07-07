using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DirectorySync.Infrastructure.Logging;

public static class RegisterLoggerExtension
{
    public static void RegisterLogger(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Serilog.Debugging.SelfLog.Enable(Console.Error);
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration);
        
        if (!builder.Environment.IsProduction() || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            loggerConfig.WriteTo.Console();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            loggerConfig
                .WriteTo.EventLog(Literals.ServiceName, "Application", manageEventSource: false);
        }

        Log.Logger = loggerConfig.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }
}
