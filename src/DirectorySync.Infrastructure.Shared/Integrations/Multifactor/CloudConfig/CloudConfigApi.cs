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
        private readonly HttpClientAdapter _adapter;

        public CloudConfigApi(HttpClient client)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _adapter = new HttpClientAdapter(client);
        }        

        public async Task<CloudConfigDto> GetConfigAsync()
        {
            var response = await _adapter.GetAsync<CloudConfigDto>(_path);
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
