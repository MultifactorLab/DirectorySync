using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Infrastructure.Http;

namespace DirectorySync.ConfigSources
{
    internal static class ConfigurationManagerExtensions
    {
        public static void AddMultifactorCloudConfiguration(this ConfigurationManager manager)
        {            
            var url = manager.GetValue<string>("Multifactor:Url");
            var key = manager.GetValue<string>("Multifactor:Key");
            var secret = manager.GetValue<string>("Multifactor:Secret");

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
            configBuilder.Add(new MultifactorCloudConfigurationSource(cli));
        }

        private static HttpClient CreateClient(Uri uri, BasicAuthHeaderValue auth)
        {
            var tracer = new HttpFallbackLogger();
            var cli = new HttpClient(tracer)
            {
                BaseAddress = uri
            };

            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

            return cli;
        }
    }
}
