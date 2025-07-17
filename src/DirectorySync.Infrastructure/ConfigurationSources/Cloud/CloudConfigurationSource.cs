using Microsoft.Extensions.Configuration;

namespace DirectorySync.Infrastructure.ConfigurationSources.Cloud;

public sealed class CloudConfigurationSource : IConfigurationSource
{
    public static CloudConfigurationProvider? CurrentProvider { get; private set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new CloudConfigurationProvider();
        CurrentProvider = provider;
        return provider;
    }
}
