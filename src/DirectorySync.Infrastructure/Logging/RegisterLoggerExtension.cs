using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Expressions;
using Serilog.Formatting.Json;

namespace DirectorySync.Infrastructure.Logging;

public class LoggingOptions
{
    public FileLoggingOptions File { get; set; } = new();
}

public class FileLoggingOptions
{
    public string? Path { get; set; }
    
    public string RollingInterval { get; set; } = "Day";
    
    // From 1 kb to 2 gb
    [Range(1024, 2L * 1024 * 1024 * 1024)]
    // default: 100 mb
    public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024;

    [Range(1, 100)]
    public int RetainedFileCountLimit { get; set; } = 20;
}

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

        Serilog.Debugging.SelfLog.Enable(Console.Error);
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext();
        
        if (!builder.Environment.IsProduction() || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            loggerConfig.WriteTo.Logger(x =>
            {
                x.WriteTo.Console(levelSwitch: new LoggingLevelSwitch(LogEventLevel.Debug),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}");
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
            path = Path.Combine(baseDir!, "log-.txt");
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
            .WriteTo.File(path: path,
                rollingInterval: rollingInterval,
                fileSizeLimitBytes: options.FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Debug));
    }
}
