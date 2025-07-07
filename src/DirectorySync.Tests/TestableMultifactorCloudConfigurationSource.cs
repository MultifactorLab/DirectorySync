using DirectorySync.Application.Models.Core;
using DirectorySync.Infrastructure.ConfigurationSources.Cloud;

namespace DirectorySync.Tests;

internal class TestableMultifactorCloudConfigurationSource : CloudConfigurationProvider
{
    public TestableMultifactorCloudConfigurationSource(HttpClient client) : base(client, TimeSpan.Zero) { }
    public IReadOnlyDictionary<string, string?> ConfigurationData => Data.AsReadOnly();
    public void SetConfigurationData(SyncSettings config) => SetData(config);
}
