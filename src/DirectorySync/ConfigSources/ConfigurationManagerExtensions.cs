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

            var tracer = new HttpTracerWihFallbackLoging();
            var cli = new HttpClient(tracer)
            {
                BaseAddress = new Uri(url)
            };

            var auth = new BasicAuthHeaderValue(key, secret);
            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

            IConfigurationBuilder configBuilder = manager;
            configBuilder.Add(new MultifactorCloudConfigurationSource(cli));
        }
    }
}
