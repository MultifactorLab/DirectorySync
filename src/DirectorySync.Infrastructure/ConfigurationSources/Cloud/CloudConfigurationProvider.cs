using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.ConfigurationProviders;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.ConfigurationSources.Cloud;

public class CloudConfigurationProvider : ConfigurationProvider, ICloudConfigurationProvider
{
    private ISyncSettingsCloudPort? _settingsCloudPort;

    public void Init(ISyncSettingsCloudPort settingsCloudPort)
    {
        _settingsCloudPort = settingsCloudPort;
        Load();
    }
    
    public override void Load()
    {
        if (_settingsCloudPort is null)
        {
            return;
        }
        
        try
        {
            var data = _settingsCloudPort
                .GetConfigAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            Data = BuildData(data);
            
            OnReload();
        }
        catch (Exception ex)
        {
            CloudInteractionLogger.Error(ex, "Failed to refresh settings from Multifactor Cloud. Local Directory Sync service settings may be out of date.");
        }
    }
    
    private static IDictionary<string, string?> BuildData(SyncSettings settings)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        SetScalar(data, "Sync:Enabled", settings.Enabled);
        SetScalar(data, "Sync:SyncTimer", settings.ScanTimer);
        SetScalar(data, "Sync:ScanTimer", settings.ScanTimer);
        SetScalar(data, "Ldap:CloudConfigRefreshTimer", settings.CloudConfigRefreshTimer);
        
        SetGroupMappings(data,"Sync:DirectoryGroupMappings", settings.DirectoryGroupMappings);
        SetArray(data,"Sync:TrackingGroups", settings.DirectoryGroupMappings.Select(c => c.DirectoryGroup).ToArray());
        SetScalar(data, "Sync:IncludeNestedGroups", "True");
        
        SetScalar(data, "Sync:PropertyMapping:IdentityAttribute", settings.PropertyMapping.IdentityAttribute);
        SetScalar(data, "Sync:PropertyMapping:NameAttribute", settings.PropertyMapping.NameAttribute);
        
        SetScalar(data, "Sync:SendEnrollmentLink", settings.SendEnrollmentLink);
        SetScalar(data, "Sync:EnrollmentLinkTtl", settings.EnrollmentLinkTtl);
        
        SetArray(data, "Sync:PropertyMapping:EmailAttributes", NormalizeOrdered(settings.PropertyMapping.EmailAttributes));

        SetArray(data, "Sync:PropertyMapping:PhoneAttributes", NormalizeOrdered(settings.PropertyMapping.PhoneAttributes));

        SetScalar(data, "Ldap:Timeout", settings.TimeoutAd);

        return data;
    }

    private static void SetScalar(IDictionary<string, string?> data, string key, object? value)
    {
        if (value is not null)
        {
            data[key] = value.ToString();
        }
    }
    
    private static void SetArray(
        IDictionary<string, string?> data,
        string key,
        IReadOnlyList<string> elements)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            data[$"{key}:{i}"] = elements[i];
        }
    }
    
    private static void SetGroupMappings(
        IDictionary<string, string?> data,
        string key,
        GroupMapping?[]? mappings)
    {
        if (mappings is null)
        {
            return;
        }

        var normalized = mappings
            .Where(m => m is not null)
            .Select(m => new
            {
                m!.DirectoryGroup,
                SignUpGroups = NormalizeOrdered(m.SignUpGroups)
            })
            .ToArray();

        for (int i = 0; i < normalized.Length; i++)
        {
            var baseKey = $"{key}:{i}";
            var m = normalized[i];

            data[$"{baseKey}:DirectoryGroup"] = m.DirectoryGroup;

            for (int j = 0; j < m.SignUpGroups.Count; j++)
            {
                data[$"{baseKey}:SignUpGroups:{j}"] = m.SignUpGroups[j];
            }
        }
    }
    
    private static IReadOnlyList<string> NormalizeOrdered(IEnumerable<string?>? source)
    {
        if (source is null)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in source)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}
