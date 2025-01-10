using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Shared.Http;

namespace DirectorySync.ConfigSources.MultifactorCloud;

internal static class ConfigurationManagerExtensions
{
    public static void AddMultifactorCloudConfiguration(this ConfigurationManager manager)
    {
        var url = manager.GetValue<string>("Multifactor:Url");
        var key = manager.GetValue<string>("Multifactor:Key");
        var secret = manager.GetValue<string>("Multifactor:Secret");
        var cloudConfigRefreshTimer = GetRefreshTimer(manager);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("Multifactor API url key should be specified in the service settings");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new Exception("Multifactor API key key should be specified in the service settings");
        }


        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new Exception("Multifactor API secret key should be specified in the service settings");
        }

        IConfigurationBuilder configBuilder = manager;
        var cli = CreateClient(new Uri(url), new BasicAuthHeaderValue(key, secret));
        configBuilder.Add(new MultifactorCloudConfigurationSource(cli, cloudConfigRefreshTimer));
    }

    private static TimeSpan GetRefreshTimer(ConfigurationManager manager)
    {
        var timer = manager.GetValue<string>("Multifactor:CloudConfigRefreshTimer");
        if (!TimeSpan.TryParseExact(timer, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var parsed))
        {
            return TimeSpan.FromMinutes(5);
        }

        if (parsed == TimeSpan.Zero)
        {
            return parsed;
        }

#if DEBUG
        return parsed;
#endif

        if (parsed != TimeSpan.Zero && parsed.TotalSeconds < 60)
        {
            return TimeSpan.FromMinutes(1);
        }

        return parsed;
    }

    private static HttpClient CreateClient(Uri uri, BasicAuthHeaderValue auth)
    {
        var tracer = new HttpCloudInteractionLogger();
        var cli = new HttpClient(tracer)
        {
            BaseAddress = uri
        };

        cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

        return cli;
    }
}
