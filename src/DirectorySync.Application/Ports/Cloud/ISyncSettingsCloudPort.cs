using DirectorySync.Application.Models;

namespace DirectorySync.Application.Ports.Cloud
{
    public interface ISyncSettingsCloudPort
    {
        Task<SyncSettings> GetConfigAsync();
    }
}
