using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using DirectorySync.Infrastructure.Logging;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;

namespace DirectorySync.ConfigSources.MultifactorCloud;

internal class MultifactorCloudConfigurationSource : ConfigurationProvider, IConfigurationSource
{
    public static string InconsistentConfigMessage { get; } = $"Group GUIDs received from the Cloud are different from local ones.{Environment.NewLine}" +
            "To confirm these changes, restart the service.";

    private readonly HttpClient _client;
    private TimeSpan _refreshTimer;
    private Timer? _timer;
    private string? _currentConfig;

    public MultifactorCloudConfigurationSource(HttpClient client, TimeSpan refreshTimer)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _refreshTimer = refreshTimer;
    }

    [DebuggerStepThrough]
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

    protected internal void SetData(CloudConfigDto config, bool initial)
    {
        if (!initial)
        {
            CheckGroups(config);
        }

        Data["Sync:Enabled"] = config.Enabled.ToString();
        Data["Sync:SyncTimer"] = config.SyncTimer.ToString();
        Data["Sync:ScanTimer"] = config.ScanTimer.ToString();

        SetCollection("Sync:DirectoryGroupMappings", config.DirectoryGroupMappings);
        SetCollection("Sync:Groups", config.DirectoryGroupMappings.Select(c => c.DirectoryGroup).ToArray());
        Data[$"Sync:IncludeNestedGroups"] = "True";

        Data["Sync:IdentityAttribute"] = config.PropertyMapping.IdentityAttribute;
        Data["Sync:NameAttribute"] = config.PropertyMapping.NameAttribute;

        Data["Sync:SendEnrollmentLink"] = config.PropertyMapping.SendEnrollmentLink.ToString();
        Data["Sync:EnrollmentLinkTtl"] = config.PropertyMapping.EnrollmentLinkTtl.ToString();

        SetCollection("Sync:EmailAttributes", config.PropertyMapping.EmailAttributes);
        SetCollection("Sync:PhoneAttributes", config.PropertyMapping.PhoneAttributes);

        Data["Ldap:Timeout"] = config.TimeoutAD.ToString();

        _refreshTimer = config.CloudConfigRefreshTimer;
        _timer?.Change(TimeSpan.Zero, _refreshTimer);
    }

    private void SetCollection(string key, string?[] elements)
    {
        for (int index = 0; index < elements.Length; index++)
        {
            Data[$"{key}:{index}"] = elements[index];
        }

        RemoveTheRestArrayItems(key, elements.Length);
    }

    private void SetCollection(string key, GroupMappingsDto?[] elements)
    {
        for (int index = 0; index < elements.Length; index++)
        {
            var baseKey = $"{key}:{index}";
            var mapping = elements[index];

            Data[$"{baseKey}:DirectoryGroup"] = mapping.DirectoryGroup;

            for (int signUpIndex = 0; signUpIndex < mapping.SignUpGroups.Length; signUpIndex++)
            {
                Data[$"{baseKey}:SignUpGroups:{signUpIndex}"] = mapping.SignUpGroups[signUpIndex];
            }

            RemoveTheRestArrayItems($"{baseKey}:SignUpGroups", mapping.SignUpGroups.Length);
        }

        RemoveTheRestArrayItems(key, elements.Length);
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

        var directoryGroups = config.DirectoryGroupMappings.Select(d => d.DirectoryGroup).ToArray();

        var currentValues = Data
            .Where(x => currentKeys.Contains(x.Key))
            .Select(x => x.Value)
            .OrderByDescending(x => x)
            .ToArray();

        if (currentValues.Any(x => !directoryGroups.Contains(x, StringComparer.OrdinalIgnoreCase)))
        {
            InconstistentEx();
        }
    }

    [DoesNotReturn]
    [DebuggerStepThrough]
    private void InconstistentEx()
    {
        throw new InconsistentConfigurationException(InconsistentConfigMessage);
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

internal sealed class InconsistentConfigurationException : Exception
{
    public InconsistentConfigurationException(string message) : base(message) { }
    public InconsistentConfigurationException(string message, Exception inner) : base(message, inner) { }
}
