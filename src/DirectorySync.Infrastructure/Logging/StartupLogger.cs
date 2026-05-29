using DirectorySync.Infrastructure.Logging.Enrichers;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace DirectorySync.Infrastructure.Logging;

/// <summary>
/// Static logger with the predefined configuration.
/// </summary>
public static class StartupLogger
{
    private const string _startupLogFile = "startup.log";
    private const long _fileSizeLimitBytes = 1024 * 1024 * 5;
    private const string _fileLogTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}";
    private const string _consoleLogTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}";

    private static readonly Lazy<Logger> _logger = new(() =>
    {
        SelfLog.Enable(Console.WriteLine);

        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With(new MfTraceIdEnricher());

        if (DirectorySyncPathBootstrap.UseEarlyLoggerFileSinks)
        {
            var dir = DirectorySyncPathBootstrap.DefaultLogDirectory();
            var path = Path.Combine(dir, _startupLogFile);
            loggerConfig = loggerConfig
                .WriteTo.File(path: path,
                    LogEventLevel.Verbose,
                    _fileLogTemplate,
                    fileSizeLimitBytes: _fileSizeLimitBytes,
                    rollOnFileSizeLimit: true);
        }

        return loggerConfig
            .WriteTo.Console(LogEventLevel.Verbose, _consoleLogTemplate)
            .CreateLogger();
    });

    /// <inheritdoc cref="Logger.Verbose"/>
    public static void Verbose(string message, params object?[] values) => _logger.Value.Verbose(message, values);

    /// <inheritdoc cref="Logger.Debug"/>
    public static void Debug(string message, params object?[] values) => _logger.Value.Debug(message, values);

    /// <inheritdoc cref="Logger.Information"/>
    public static void Information(string message, params object?[] values) => _logger.Value.Information(message, values);

    /// <inheritdoc cref="Logger.Error"/>
    public static void Error(string message, params object?[] values) => _logger.Value.Error(message, values);

    /// <inheritdoc cref="Logger.Error"/>
    public static void Error(Exception ex, string message, params object?[] values) => _logger.Value.Error(ex, message, values);
}
