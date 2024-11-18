using DirectorySync.ConfigSources.MultifactorCloud;

namespace DirectorySync.Tests
{
    internal class TestableMultifactorCloudConfigurationSource : MultifactorCloudConfigurationSource
    {
        public TestableMultifactorCloudConfigurationSource(HttpClient client) : base(client, TimeSpan.Zero) { }
        public IReadOnlyDictionary<string, string?> ConfigurationData => Data.AsReadOnly();
    }
}
