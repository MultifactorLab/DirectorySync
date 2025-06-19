using DirectorySync.Application.Models.Options;

namespace DirectorySync.Application.Ports.Cloud;

public interface ISyncSettingsCloudPort
{
    Task<SyncSettings> GetConfigAsync(CancellationToken cancellationToken = default);
}
