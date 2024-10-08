﻿using DirectorySync.ConfigSources;

namespace DirectorySync.Tests
{
    internal class TestableMultifactorCloudConfigurationSource : MultifactorCloudConfigurationSource
    {
        public TestableMultifactorCloudConfigurationSource(HttpClient client) : base(client) { }
        public IReadOnlyDictionary<string, string?> ConfigurationData => Data.AsReadOnly();
    }
}
