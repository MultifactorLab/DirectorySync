using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Configurations;

/// <summary>
/// Persistence locations for the runtime. When <see cref="Directory"/> is empty, the host uses
/// the Windows Service profile folder only in <see cref="DirectorySync.Hosting.DirectorySyncRuntimeMode.WindowsService"/>;
/// all other modes use <c>./data</c> under <see cref="AppContext.BaseDirectory"/>.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Base directory for LiteDB and related files. Relative paths are resolved from <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public string? Directory { get; init; }

    /// <summary>
    /// LiteDB file name (not a full path), combined with <see cref="Directory"/> or the platform default directory.
    /// </summary>
    [MinLength(1)]
    public string LiteDbFileName { get; init; } = "storage.db";
}
