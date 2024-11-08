using Serilog.Events;
using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Logging;

public class LoggingOptions
{
    public FileLoggingOptions File { get; set; } = new();
    public ConsoleLoggingOptions Console { get; set; } = new();
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

    public string MinimalLevel { get; set; } = LogEventLevel.Debug.ToString();

    public string Template { get; set; }
}

public class ConsoleLoggingOptions
{
    public string MinimalLevel { get; set; } = LogEventLevel.Debug.ToString();

    public string Template { get; set; }
}
