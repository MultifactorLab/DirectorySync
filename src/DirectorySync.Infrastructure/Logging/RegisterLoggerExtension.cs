using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace DirectorySync.Infrastructure.Logging;

public static class RegisterLoggerExtension
{
    public static void RegisterLogger(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Configuration.GetSection("Logging")
            .Get<LoggingOptions>(x => x.BindNonPublicProperties = true);
        if (options is null)
        {
            throw new Exception("Unable to read logging option");
        }

        DataAnnotationsValidator.Validate(options);

        SelfLog.Enable(Console.Error);
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext();
        
        if (!builder.Environment.IsProduction() || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            loggerConfig.WriteTo.Logger(x =>
            {
                x.WriteTo.Console(levelSwitch: new LoggingLevelSwitch(LogEventLevel.Debug),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}");
            });
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            loggerConfig
                .WriteTo.Logger(x =>
                {
                    x.Filter.ByIncludingOnly("SourceContext like 'DirectorySync%' or CustomSourceContext like 'DirectorySync%'");
                    x.WriteTo.EventLog(source: Literals.ServiceName, 
                        logName: "Application", 
                        manageEventSource: false, 
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        eventIdProvider: new DirectorySyncEventIdProvider());
                });
        }

        ConfigureFileLogging(loggerConfig, options.File);

        Log.Logger = loggerConfig.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }

    private static void ConfigureFileLogging(LoggerConfiguration logger, FileLoggingOptions options)
    {
        string path;
        if (!string.IsNullOrWhiteSpace(options.Path))
        {
            path = options.Path;
        }
        else
        {
            var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            path = Path.Combine(baseDir!, "logs", "log-.txt");
        }

        RollingInterval rollingInterval;
        if (Enum.TryParse<RollingInterval>(options.RollingInterval, true, out var parsedInterval))
        {
            rollingInterval = parsedInterval;
        }
        else
        {
            rollingInterval = RollingInterval.Day;
        }

        logger
            .WriteTo.Logger(x =>
            {
                x.WriteTo.File(path: path,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}",
                    rollingInterval: rollingInterval,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    levelSwitch: new LoggingLevelSwitch(LogEventLevel.Debug));
            });
    }
}
