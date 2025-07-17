using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Infrastructure.Dto.Multifactor.SyncSettings;
using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Adapters.Multifactor;

public class MultifactorSyncSettingsApi : ISyncSettingsCloudPort
{
    private const string _clientName = "MultifactorApi";

    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MultifactorUsersApi> _logger;

    public MultifactorSyncSettingsApi(IHttpClientFactory clientFactory,
       ILogger<MultifactorUsersApi> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<SyncSettings> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(client);
        var response = await adapter.GetAsync<CloudConfigDto>("v2/ds/settings");
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
