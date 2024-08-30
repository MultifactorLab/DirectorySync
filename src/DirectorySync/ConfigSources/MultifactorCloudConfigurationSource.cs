using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;
using DirectorySync.Application.Integrations.Multifactor.Http;
using DirectorySync.Exceptions;
using DirectorySync.Infrastructure.Logging;

namespace DirectorySync.ConfigSources
{
    internal class MultifactorCloudConfigurationSource : ConfigurationProvider, IConfigurationSource
    {
        private readonly HttpClient _client;

        public MultifactorCloudConfigurationSource(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public override void Load()
        {
            FallbackLogger.Information("Pulling settings from Multifactor Cloud");
            
            var adapter = new HttpClientAdapter(_client);
            var response = adapter.GetAsync<DirectorySyncSettingsDto>("ds/settings").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new PullMultifactorSettingsException("Failed to pull settings from Multifactor Cloud", response);
            }

            var dto = response.Model;
            if (dto is null)
            {
                throw new PullMultifactorSettingsException("Empty config was retrieved from Multifactor Cloud", response);
            }

            Data["Sync:Enabled"] = dto.Enabled.ToString();
            Data["Sync:SyncTimer"] = dto.SyncTimer.ToString();
            Data["Sync:ScanTimer"] = dto.ScanTimer.ToString();

            for (int index = 0; index < dto.DirectoryGroups.Length; index++)
            {
                Data[$"Sync:Groups:{index}"] = dto.DirectoryGroups[index];
            }
            
            Data["Sync:IdentityAttribute"] = dto.PropertyMapping.IdentityAttribute;
            Data["Sync:NameAttribute"] = dto.PropertyMapping.NameAttribute;
            
            for (int index = 0; index < dto.PropertyMapping.EmailAttributes.Length; index++)
            {
                Data[$"Sync:EmailAttributes:{index}"] = dto.PropertyMapping.EmailAttributes[index];
            }
            
            for (int index = 0; index < dto.PropertyMapping.PhoneAttributes.Length; index++)
            {
                Data[$"Sync:PhoneAttributes:{index}"] = dto.PropertyMapping.PhoneAttributes[index];
            }
            
            for (int index = 0; index < dto.MultifactorGroupPolicyPreset.SignUpGroups.Length; index++)
            {
                Data[$"Sync:MultifactorGroupPolicyPreset:SignUpGroups:{index}"] = 
                    dto.MultifactorGroupPolicyPreset.SignUpGroups[index];
            }
            
            FallbackLogger.Information("Settings were pulled from Multifactor Cloud");
        }
    }
    
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

            var cli = new HttpClient()
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
