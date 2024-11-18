using DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;
using DirectorySync.Exceptions;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Logging;
using System.Text.Json;

namespace DirectorySync.ConfigSources.MultifactorCloud;

internal class MultifactorCloudConfigurationSource : ConfigurationProvider, IConfigurationSource
{
    private readonly HttpClient _client;
    private TimeSpan _refreshTimer;
    private Timer? _timer;
    private string? _currentConfig;

    public MultifactorCloudConfigurationSource(HttpClient client, TimeSpan refreshTimer)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _refreshTimer = refreshTimer;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return this;
    }

    public override void Load()
    {
        LoadCloudConfigData();

        if (_refreshTimer == TimeSpan.Zero)
        {
            return;
        }

        Task.Run(async () =>
        {
            await Task.Delay(_refreshTimer);
            _timer = new Timer(Refresh, null, TimeSpan.Zero, _refreshTimer);
        });
    }

    private void Refresh(object? state)
    {
        try
        {
            LoadCloudConfigData();
        }
        catch (Exception ex)
        {
            CloudInteractionLogger.Error(ex, "Failed to refresh settings from Multifactor Cloud. Local Directory Sync service settings may be out of date.");
        }
    }

    private CloudConfigDto Pull()
    {
        CloudInteractionLogger.Verbose("Pulling settings from Multifactor Cloud");

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

        CloudInteractionLogger.Verbose("Settings pulled from Multifactor Cloud");

        return dto;
    }

    private void LoadCloudConfigData()
    {
        var config = Pull();
        if (!HasChanges(config))
        {
            return;
        }

        SetData(config);
        Remember(config);

        OnReload();
        CloudInteractionLogger.Information("Cloud settings was changed");
    }

    private void SetData(CloudConfigDto config)
    {
        Data["Sync:Enabled"] = config.Enabled.ToString();
        Data["Sync:SyncTimer"] = config.SyncTimer.ToString();
        Data["Sync:ScanTimer"] = config.ScanTimer.ToString();

        for (int index = 0; index < config.DirectoryGroups.Length; index++)
        {
            Data[$"Sync:Groups:{index}"] = config.DirectoryGroups[index];
        }

        Data["Sync:IdentityAttribute"] = config.PropertyMapping.IdentityAttribute;
        Data["Sync:NameAttribute"] = config.PropertyMapping.NameAttribute;

        for (int index = 0; index < config.PropertyMapping.EmailAttributes.Length; index++)
        {
            Data[$"Sync:EmailAttributes:{index}"] = config.PropertyMapping.EmailAttributes[index];
        }

        for (int index = 0; index < config.PropertyMapping.PhoneAttributes.Length; index++)
        {
            Data[$"Sync:PhoneAttributes:{index}"] = config.PropertyMapping.PhoneAttributes[index];
        }

        _refreshTimer = config.CloudConfigRefreshTimer;
        _timer?.Change(TimeSpan.Zero, _refreshTimer);
    }

    private bool HasChanges(CloudConfigDto newConfig)
    {
        var json = JsonSerializer.Serialize(newConfig);
        return !json.Equals(_currentConfig);
    }

    private void Remember(CloudConfigDto config)
    {
        _currentConfig = JsonSerializer.Serialize(config);
    }
}
