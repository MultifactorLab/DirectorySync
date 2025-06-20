using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.ConfigurationProviders;
using DirectorySync.Application.Ports.Options;

namespace DirectorySync.Application.UseCases;

public interface ISynchronizeCloudSettingsUseCase
{
    Task ExecuteAsync(bool isInit, ICloudConfigurationProvider provider, CancellationToken cancellationToken = default);
}

public class SynchronizeCloudSettingsUseCase : ISynchronizeCloudSettingsUseCase
{
    private readonly ISyncSettingsOptions _syncSettingsOptions;

    public SynchronizeCloudSettingsUseCase(ISyncSettingsOptions syncSettingsOptions,
        ISyncSettingsCloudPort settingsCloudPort)
    {
        _syncSettingsOptions = syncSettingsOptions;
    }

    public async Task ExecuteAsync(bool isInit,
        ICloudConfigurationProvider provider,
        CancellationToken cancellationToken = default)
    {
        
    }
}
