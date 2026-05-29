using System.Runtime.InteropServices;
using DirectorySync.Hosting;

namespace DirectorySync.Infrastructure.Configurations;

/// <summary>
/// Resolves storage and log paths relative to <see cref="AppContext.BaseDirectory"/> with
/// Windows Service–specific defaults for LiteDB when <see cref="StorageOptions.Directory"/> is unset.
/// </summary>
public static class ApplicationPathResolver
{
    public static string BaseDirectory => AppContext.BaseDirectory;

    /// <summary>
    /// Turns a configured path into an absolute path. Relative entries are rooted at <see cref="BaseDirectory"/>.
    /// </summary>
    public static string ResolvePathRelativeToBase(string pathOrRelative)
    {
        var trimmed = pathOrRelative.Trim();
        return Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(BaseDirectory, trimmed));
    }

    /// <summary>
    /// LiteDB directory: explicit <see cref="StorageOptions.Directory"/>, otherwise Windows Service profile path on Windows,
    /// otherwise <c>./data</c> under the application base directory.
    /// </summary>
    public static string ResolveStorageBaseDirectory(StorageOptions storage, DirectorySyncRuntimeMode runtimeMode)
    {
        if (!string.IsNullOrWhiteSpace(storage.Directory))
        {
            return ResolvePathRelativeToBase(storage.Directory!);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && runtimeMode == DirectorySyncRuntimeMode.WindowsService)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Multifactor",
                "Directory Sync");
        }

        return Path.GetFullPath(Path.Combine(BaseDirectory, "data"));
    }

    /// <summary>
    /// Serilog rolling file path. When <paramref name="configuredPath"/> is empty, uses <c>./logs/log-.txt</c> under base.
    /// </summary>
    public static string ResolveRollingLogFilePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var trimmed = configuredPath.Trim();
            return Path.IsPathRooted(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(BaseDirectory, trimmed));
        }

        var logDir = Path.GetFullPath(Path.Combine(BaseDirectory, "logs"));
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "log-.txt");
    }

    public static void EnsureParentDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
