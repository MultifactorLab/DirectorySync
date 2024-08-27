using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace DirectorySync.Infrastructure.Logging;

public static class FallbackLogger
{
    private static readonly Logger _logger;

    static FallbackLogger()
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
                rollOnFileSizeLimit: true);

        _logger = loggerConfig.CreateLogger();
    }
    
    public static void Verbose(string message, params object?[] values) => _logger.Verbose(message, values);
    
    public static void Information(string message, params object?[] values) => _logger.Information(message, values);
    
    
    public static void Error(string message, params object?[] values) => _logger.Error(message, values);
    
    public static void Error(Exception ex, string message, params object?[] values) => _logger.Error(ex, message, values);
}
