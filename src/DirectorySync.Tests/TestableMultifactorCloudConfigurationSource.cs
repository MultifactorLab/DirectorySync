using DirectorySync.Infrastructure.ConfigurationSources.MultifactorCloud;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;

namespace DirectorySync.Tests;

internal class TestableMultifactorCloudConfigurationSource : MultifactorCloudConfigurationSource
{
    public TestableMultifactorCloudConfigurationSource(HttpClient client) : base(client, TimeSpan.Zero) { }
    public IReadOnlyDictionary<string, string?> ConfigurationData => Data.AsReadOnly();
    public void SetConfigurationData(CloudConfigDto config, bool initial) => SetData(config, initial);
}
