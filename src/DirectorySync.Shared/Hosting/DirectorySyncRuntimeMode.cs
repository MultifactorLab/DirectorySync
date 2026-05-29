namespace DirectorySync.Hosting;

/// <summary>
/// Identifies how the process is hosted so shared composition can apply the correct
/// platform integrations without scattering <see cref="System.Runtime.InteropServices.RuntimeInformation"/> checks.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Console"/>, <see cref="LinuxSystemd"/>, and <see cref="Docker"/> use the default generic-host foreground lifetime.</description></item>
/// <item><description><see cref="WindowsService"/> is the Windows SCM adapter: registers Windows Service integration only in that mode.</description></item>
/// <item><description>Deployment adapters (WiX, systemd units, Docker images) choose the executable and pass the matching mode; they must not embed business or sync rules.</description></item>
/// </list>
/// </remarks>
public enum DirectorySyncRuntimeMode
{
    /// <summary>Interactive or headless foreground host (e.g. <c>dotnet run</c>, future Linux console host).</summary>
    Console = 0,

    /// <summary>Managed by systemd as a foreground unit; same host lifetime as <see cref="Console"/>; reserved for path/logging policy.</summary>
    LinuxSystemd = 1,

    /// <summary>Container foreground process; same host lifetime as <see cref="Console"/>; reserved for path/logging policy.</summary>
    Docker = 2,

    /// <summary>Windows Service (SCM): enables Windows Service lifetime and Windows installer–aligned configuration.</summary>
    WindowsService = 3,
}
