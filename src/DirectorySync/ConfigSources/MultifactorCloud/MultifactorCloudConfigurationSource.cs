using DirectorySync.Infrastructure.Logging;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        LoadCloudConfigData(true);

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
            LoadCloudConfigData(false);
        }
        catch (Exception ex)
        {
            CloudInteractionLogger.Error(ex, "Failed to refresh settings from Multifactor Cloud. Local Directory Sync service settings may be out of date.");
        }
    }

    private CloudConfigDto Pull()
    {
        CloudInteractionLogger.Verbose("Pulling settings from Multifactor Cloud");

        var api = new CloudConfigApi(_client);
        var dto = api.GetConfigAsync().GetAwaiter().GetResult();

        CloudInteractionLogger.Verbose("Settings pulled from Multifactor Cloud");

        return dto;
    }

    private void LoadCloudConfigData(bool initial)
    {
        var config = Pull();
        if (!HasChanges(config))
        {
            return;
        }

        SetData(config, initial);
        Remember(config);

        OnReload();
        CloudInteractionLogger.Information("Cloud settings was changed");
    }

    private void SetData(CloudConfigDto config, bool initial)
    {
        if (!initial)
        {
            CheckGroups(config);
        }

        Data["Sync:Enabled"] = config.Enabled.ToString();
        Data["Sync:SyncTimer"] = config.SyncTimer.ToString();
        Data["Sync:ScanTimer"] = config.ScanTimer.ToString();

        for (int index = 0; index < config.DirectoryGroups.Length; index++)
        {
            Data[$"Sync:Groups:{index}"] = config.DirectoryGroups[index];
        }
        RemoveTheRestArrayItems("Sync:Groups", config.PropertyMapping.EmailAttributes.Length - 1);
        Data[$"Sync:IncludeNestedGroups"] = config.IncludeNestedDirectoryGroups.ToString();

        Data["Sync:IdentityAttribute"] = config.PropertyMapping.IdentityAttribute;
        Data["Sync:NameAttribute"] = config.PropertyMapping.NameAttribute;

        for (int index = 0; index < config.PropertyMapping.EmailAttributes.Length; index++)
        {
            Data[$"Sync:EmailAttributes:{index}"] = config.PropertyMapping.EmailAttributes[index];
        }
        RemoveTheRestArrayItems("Sync:EmailAttributes", config.PropertyMapping.EmailAttributes.Length - 1);

        for (int index = 0; index < config.PropertyMapping.PhoneAttributes.Length; index++)
        {
            Data[$"Sync:PhoneAttributes:{index}"] = config.PropertyMapping.PhoneAttributes[index];
        }
        RemoveTheRestArrayItems("Sync:PhoneAttributes", config.PropertyMapping.EmailAttributes.Length - 1);

        _refreshTimer = config.CloudConfigRefreshTimer;
        _timer?.Change(TimeSpan.Zero, _refreshTimer);
    }

    private void RemoveTheRestArrayItems(string key, int startIndex)
    {
        while (true)
        {
            var k = $"{key}:{startIndex}";
            if (!Data.TryGetValue(k, out var _))
            {
                return;
            }

            Data.Remove(k);
            startIndex++;
        }
    }

    private void CheckGroups(CloudConfigDto config)
    {
        var r = new Regex("^Sync:Groups:\\d$+");
        var currentKeys = Data.Keys
            .Where(x => r.IsMatch(x))
            .ToArray();

        if (currentKeys.Length != config.DirectoryGroups.Length)
        {
            throw new Exception("Group GUIDs received from the Cloud are different from local ones");
        }

        var currentValues = Data
            .Where(x => currentKeys.Contains(x.Key))
            .Select(x => x.Value)
            .OrderByDescending(x => x)
            .ToArray();

        if (currentValues.Any(x => !config.DirectoryGroups.Contains(x, StringComparer.OrdinalIgnoreCase)))
        {
            throw new Exception("Group GUIDs received from the Cloud are different from local ones");
        }
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
