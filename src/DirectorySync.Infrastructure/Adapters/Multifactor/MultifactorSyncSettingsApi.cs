using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Infrastructure.Dto.Cloud.SyncSettings;
using DirectorySync.Infrastructure.Dto.Multifactor.SyncSettings;
using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;

namespace DirectorySync.Infrastructure.Adapters.Multifactor;

public class MultifactorSyncSettingsApi : ISyncSettingsCloudPort
{
    const string _path = "v2/ds/settings";
    private readonly HttpClientAdapter _adapter;

    public MultifactorSyncSettingsApi(HttpClient client)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        _adapter = new HttpClientAdapter(client);
    }        

    public async Task<SyncSettings> GetConfigAsync(CancellationToken cancellationToken = default)
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

        return CloudConfigDto.ToModel(dto);
    }
}
