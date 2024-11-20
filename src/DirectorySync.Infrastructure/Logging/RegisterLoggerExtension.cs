using System.Runtime.InteropServices;
using DirectorySync.Infrastructure.Logging.Enrichers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
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

        SelfLog.Enable(Console.WriteLine);
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With(new MfTraceIdEnricher())
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
        
        if (!builder.Environment.IsProduction() || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ConfigureConsoleLogging(loggerConfig, options.Console);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ConfigureEventLogger(loggerConfig);
        }

        ConfigureFileLogging(loggerConfig, options.File);

        Log.Logger = loggerConfig.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }

    private static void ConfigureConsoleLogging(LoggerConfiguration logger, ConsoleLoggingOptions options)
    {
        var minimalLevel = GetMinimalLevel(options.MinimalLevel);
        var consoleTemplate = !string.IsNullOrWhiteSpace(options.Template)
            ? options.Template
            : "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}";

        logger.WriteTo.Logger(x =>
        {
            x.WriteTo.Console(restrictedToMinimumLevel: minimalLevel,
                outputTemplate: consoleTemplate);
        });
    }

    private static void ConfigureEventLogger(LoggerConfiguration logger)
    {
        logger
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

    private static void ConfigureFileLogging(LoggerConfiguration logger, FileLoggingOptions options)
    {
        var path = GetLogFilePath(options);
        var rollingInterval = GetInterval(options);
        var minimalLevel = GetMinimalLevel(options.MinimalLevel);
        var fileTemplate = !string.IsNullOrWhiteSpace(options.Template)
            ? options.Template
            : "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}";

        logger
            .WriteTo.Logger(x =>
            {
                x.WriteTo.File(path: path,
                    outputTemplate: fileTemplate,
                    rollingInterval: rollingInterval,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    restrictedToMinimumLevel: minimalLevel);                
            });
    }

    private static string GetLogFilePath(FileLoggingOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Path))
        {
            return options.Path;
        }
        
        var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        var dir = Path.Combine(baseDir!, "logs");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "log-.txt");
    }
    
    private static RollingInterval GetInterval(FileLoggingOptions options)
    {
        if (Enum.TryParse<RollingInterval>(options.RollingInterval, true, out var parsedInterval))
        {
            return parsedInterval;
        } 
        
        return RollingInterval.Day;
    }    
    
    private static LogEventLevel GetMinimalLevel(string minimalLevel)
    {
        if (Enum.TryParse<LogEventLevel>(minimalLevel, true, out var parsedMinimalLevel))
        {
            return parsedMinimalLevel;
        }

        return LogEventLevel.Debug;
    }
}
