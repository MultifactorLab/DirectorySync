using System.Runtime.InteropServices;
using DirectorySync.Hosting;
using DirectorySync.Infrastructure.Configurations;
using FluentAssertions;

namespace DirectorySync.Infrastructure.Tests.Configurations;

public class ApplicationPathResolverTests
{
    [Fact]
    public void ResolvePathRelativeToBase_ResolvesRelativeAgainstBase()
    {
        var path = ApplicationPathResolver.ResolvePathRelativeToBase(Path.Combine("a", "b.txt"));
        path.Should().Be(Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "a", "b.txt")));
    }

    [Fact]
    public void ResolveRollingLogFilePath_Empty_CreatesLogsUnderBase()
    {
        var path = ApplicationPathResolver.ResolveRollingLogFilePath(null);
        path.Should().EndWith(Path.Combine("logs", "log-.txt"));
        Directory.Exists(Path.GetDirectoryName(path)!).Should().BeTrue();
    }

    [Fact]
    public void ResolveRollingLogFilePath_Relative_IsUnderBase()
    {
        var path = ApplicationPathResolver.ResolveRollingLogFilePath(Path.Combine("logs", "app-.txt"));
        path.Should().Be(Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "logs", "app-.txt")));
    }

    [Fact]
    public void ResolveStorageBaseDirectory_WithDirectory_UsesConfiguredPath()
    {
        var storage = new StorageOptions { Directory = "custom-data", LiteDbFileName = "db.db" };
        var dir = ApplicationPathResolver.ResolveStorageBaseDirectory(storage, DirectorySyncRuntimeMode.Console);
        dir.Should().Be(Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "custom-data")));
    }

    [Fact]
    public void ResolveStorageBaseDirectory_Empty_OnNonWindows_UsesDataUnderBase()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var storage = new StorageOptions { Directory = null, LiteDbFileName = "x.db" };
        var dir = ApplicationPathResolver.ResolveStorageBaseDirectory(storage, DirectorySyncRuntimeMode.Console);
        dir.Should().Be(Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "data")));
    }

    [Fact]
    public void ResolveStorageBaseDirectory_Empty_WindowsService_OnWindows_UsesProfileSubfolder()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var storage = new StorageOptions { Directory = null, LiteDbFileName = "x.db" };
        var dir = ApplicationPathResolver.ResolveStorageBaseDirectory(storage, DirectorySyncRuntimeMode.WindowsService);
        dir.Should().Contain("Directory Sync");
    }

    [Fact]
    public void ResolveStorageBaseDirectory_Empty_ConsoleOnWindows_UsesDataUnderBase()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var storage = new StorageOptions { Directory = null, LiteDbFileName = "x.db" };
        var dir = ApplicationPathResolver.ResolveStorageBaseDirectory(storage, DirectorySyncRuntimeMode.Console);
        dir.Should().Be(Path.GetFullPath(Path.Combine(ApplicationPathResolver.BaseDirectory, "data")));
    }
}
