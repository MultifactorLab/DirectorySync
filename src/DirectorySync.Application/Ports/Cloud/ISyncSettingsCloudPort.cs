using DirectorySync.Application.Models.Core;

namespace DirectorySync.Application.Ports.Cloud;

public interface ISyncSettingsCloudPort
{
    Task<SyncSettings> GetConfigAsync(CancellationToken cancellationToken = default);
}
