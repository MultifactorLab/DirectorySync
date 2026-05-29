namespace DirectorySync.Hosting;

/// <summary>
/// Options for <see cref="DirectorySyncHost.Configure"/> supplied by an executable under <c>hosts/*</c>.
/// </summary>
public sealed class DirectorySyncHostingOptions
{
    /// <summary>How this process is intended to run; drives Windows Service registration and related adapters.</summary>
    public DirectorySyncRuntimeMode RuntimeMode { get; init; }

    /// <summary>
    /// When true, registers configuration from machine-level environment variables (Windows MSI compatibility).
    /// Ignored on non-Windows platforms.
    /// </summary>
    public bool IncludeWindowsMachineEnvironmentConfiguration { get; init; }

    /// <summary>Current shipped Windows Service / MSI entrypoint defaults.</summary>
    public static DirectorySyncHostingOptions WindowsServiceEntry { get; } = new()
    {
        RuntimeMode = DirectorySyncRuntimeMode.WindowsService,
        IncludeWindowsMachineEnvironmentConfiguration = true,
    };

    /// <summary>Interactive or headless foreground host when not under systemd or Docker.</summary>
    public static DirectorySyncHostingOptions ForegroundEntry { get; } = new()
    {
        RuntimeMode = DirectorySyncRuntimeMode.Console,
        IncludeWindowsMachineEnvironmentConfiguration = false,
    };

    /// <summary>Linux systemd unit foreground process (same lifetime as console; used for logging/path policy).</summary>
    public static DirectorySyncHostingOptions LinuxSystemdForegroundEntry { get; } = new()
    {
        RuntimeMode = DirectorySyncRuntimeMode.LinuxSystemd,
        IncludeWindowsMachineEnvironmentConfiguration = false,
    };

    /// <summary>Container foreground process (same lifetime as console; used for logging/path policy).</summary>
    public static DirectorySyncHostingOptions DockerForegroundEntry { get; } = new()
    {
        RuntimeMode = DirectorySyncRuntimeMode.Docker,
        IncludeWindowsMachineEnvironmentConfiguration = false,
    };

    /// <summary>
    /// Picks <see cref="DockerForegroundEntry"/>, <see cref="LinuxSystemdForegroundEntry"/>, or <see cref="ForegroundEntry"/>
    /// for <see cref="DirectorySyncRuntimeMode"/> based on common deployment signals.
    /// </summary>
    public static DirectorySyncHostingOptions ResolveForegroundHostEntry()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase)
            || File.Exists("/.dockerenv"))
        {
            return DockerForegroundEntry;
        }

        if (OperatingSystem.IsLinux()
            && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INVOCATION_ID")))
        {
            return LinuxSystemdForegroundEntry;
        }

        return ForegroundEntry;
    }
}
