using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.ConfigurationProviders;
using DirectorySync.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;

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
            var data = _settingsCloudPort.GetConfigAsync().GetAwaiter().GetResult();
            SetData(data);
        }
        catch (Exception ex)
        {
            CloudInteractionLogger.Error(ex, "Failed to refresh settings from Multifactor Cloud. Local Directory Sync service settings may be out of date.");
        }
        
    }

    private void SetData(SyncSettings settings)
    {
        Data["Sync:Enabled"] = settings.Enabled.ToString();
        Data["Sync:SyncTimer"] = settings.SyncTimer.ToString();
        Data["Sync:ScanTimer"] = settings.ScanTimer.ToString();
        Data["Sync:CloudConfigRefreshTimer"] = settings.CloudConfigRefreshTimer.ToString();
        

        SetCollection("Sync:DirectoryGroupMappings", settings.DirectoryGroupMappings);
        SetCollection("Sync:TrackingGroups", settings.DirectoryGroupMappings.Select(c => c.DirectoryGroup).ToArray());
        Data[$"Sync:IncludeNestedGroups"] = "True";

        Data["Sync:PropertyMapping:IdentityAttribute"] = settings.PropertyMapping.IdentityAttribute;
        Data["Sync:PropertyMapping:NameAttribute"] = settings.PropertyMapping.NameAttribute;

        Data["Sync:SendEnrollmentLink"] = settings.SendEnrollmentLink.ToString();
        Data["Sync:EnrollmentLinkTtl"] = settings.EnrollmentLinkTtl.ToString();

        SetCollection("Sync:PropertyMapping:EmailAttributes", settings.PropertyMapping.EmailAttributes);
        SetCollection("Sync:PropertyMapping:PhoneAttributes", settings.PropertyMapping.PhoneAttributes);

        Data["Ldap:Timeout"] = settings.TimeoutAd.ToString();
        
        OnReload();
    }
    
    private void SetCollection(string key, string?[] elements)
    {
        ResetCollection(key);
        
        for (int index = 0; index < elements.Length; index++)
        {
            Data[$"{key}:{index}"] = elements[index];
        }
    }

    private void SetCollection(string key, GroupMapping?[] elements)
    {
        ResetCollection(key);
        
        for (int index = 0; index < elements.Length; index++)
        {
            var baseKey = $"{key}:{index}";
            var mapping = elements[index];

            Data[$"{baseKey}:DirectoryGroup"] = mapping.DirectoryGroup;

            for (int signUpIndex = 0; signUpIndex < mapping.SignUpGroups.Length; signUpIndex++)
            {
                Data[$"{baseKey}:SignUpGroups:{signUpIndex}"] = mapping.SignUpGroups[signUpIndex];
            }
        }
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
    
    private void ResetCollection(string prefix)
    {
        var keysToRemove = Data.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var k in keysToRemove)
        {
            Data.Remove(k);
        }
    }
}
