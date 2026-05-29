using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Configurations;

namespace DirectorySync.Infrastructure.Logging;

/// <summary>
/// Captures hosting mode before DI completes so static early loggers can match main Serilog path policy.
/// </summary>
public static class DirectorySyncPathBootstrap
{
    private static DirectorySyncRuntimeMode? _runtimeMode;

    public static void Initialize(DirectorySyncRuntimeMode runtimeMode)
    {
        _runtimeMode = runtimeMode;
    }

    public static DirectorySyncRuntimeMode RuntimeMode =>
        _runtimeMode ?? DirectorySyncRuntimeMode.Console;

    /// <summary>When false, early loggers skip file sinks (Docker / read-only image friendly).</summary>
    public static bool UseEarlyLoggerFileSinks => RuntimeMode != DirectorySyncRuntimeMode.Docker;

    public static string DefaultLogDirectory()
    {
        var dir = Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "logs"));
        if (UseEarlyLoggerFileSinks && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return dir;
    }
}
