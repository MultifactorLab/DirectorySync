using DirectorySync.Infrastructure.Logging.Enrichers;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace DirectorySync.Infrastructure.Logging;

/// <summary>
/// Static logger with the predefined configuration.
/// </summary>
public static class FallbackLogger
{
    private static readonly Lazy<Logger> _logger = new(() =>
    {
        SelfLog.Enable(Console.WriteLine);

        var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        var dir = Path.Combine(baseDir!, "logs");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.Combine(dir, "startup.log");
        var loggerConfig = new LoggerConfiguration()

            .WriteTo.File(path: path,
                LogEventLevel.Verbose,
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}",
                fileSizeLimitBytes: 1024 * 1024 * 5,
                rollOnFileSizeLimit: true)

            .WriteTo.Console(LogEventLevel.Verbose, 
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}")

            .Enrich.FromLogContext()

            .Enrich.With(new MfTraceIdEnricher());

        return loggerConfig.CreateLogger();
    });

    /// <inheritdoc cref="Logger.Verbose"/>
    public static void Verbose(string message, params object?[] values) => _logger.Value.Verbose(message, values);

    /// <inheritdoc cref="Logger.Information"/>
    public static void Information(string message, params object?[] values) => _logger.Value.Information(message, values);

    /// <inheritdoc cref="Logger.Error"/>
    public static void Error(string message, params object?[] values) => _logger.Value.Error(message, values);

    /// <inheritdoc cref="Logger.Error"/>
    public static void Error(Exception ex, string message, params object?[] values) => _logger.Value.Error(ex, message, values);
}
