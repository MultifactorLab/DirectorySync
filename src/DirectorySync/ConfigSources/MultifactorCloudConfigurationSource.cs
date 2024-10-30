using DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;
using DirectorySync.Exceptions;
using DirectorySync.Infrastructure.Http;
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
            var response = adapter.GetAsync<CloudConfigDto>("ds/settings").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new PullCloudConfigException("Failed to pull settings from Multifactor Cloud", response);
            }

            var dto = response.Model;
            if (dto is null)
            {
                throw new PullCloudConfigException("Empty config was retrieved from Multifactor Cloud", response);
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
            
            FallbackLogger.Information("Settings pulled from Multifactor Cloud");
        }
    }
}
