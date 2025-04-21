using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace DirectorySync.Infrastructure.Tests;

internal static class MockExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times, string? regex = null)
    {
        logger.Verify(
            m => m.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((x, y) => regex == null || Regex.IsMatch(x.ToString()!, regex)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public static IServiceCollection AddMockedConfigurationWithSyncDisabled(this IServiceCollection services)
    {
        var memorySource = new MemoryConfigurationSource
        {
            InitialData = new[]
            {
                new KeyValuePair<string, string?>("Sync:Enabled", "false")
            }
        };

        var memoryProvider = new MemoryConfigurationProvider(memorySource);

        var configRootMock = new Mock<IConfigurationRoot>();
        configRootMock
            .SetupGet(c => c.Providers)
            .Returns(new List<IConfigurationProvider> { memoryProvider });

        configRootMock
            .Setup(c => c.GetSection("Sync:Enabled"))
            .Returns(new ConfigurationSection(new ConfigurationRoot(new[] { memoryProvider }), "Sync:Enabled"));

        services.AddSingleton<IConfiguration>(configRootMock.Object);
        services.AddSingleton(configRootMock.Object);

        return services;
    }
}
