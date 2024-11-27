using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig
{
    public sealed class CloudConfigApi
    {
        const string _path = "ds/settings";
        private readonly HttpClient _client;

        public CloudConfigApi(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }        

        public async Task<CloudConfigDto> GetConfigAsync()
        {
            var adapter = new HttpClientAdapter(_client);
            var response = await adapter.GetAsync<CloudConfigDto>(_path);
            if (!response.IsSuccessStatusCode)
            {
                throw new PullCloudConfigException("Failed to pull settings from Multifactor Cloud", response);
            }

            var dto = response.Model;
            if (dto == null)
            {
                throw new PullCloudConfigException("Empty config was retrieved from Multifactor Cloud", response);
            }

            return dto;
        }
    }
}
