using DirectorySync.Application.Models.Options;

namespace DirectorySync.Application.Ports.Databases;

public interface ISyncSettingsDatabase
{
    SyncSettings GetSyncSettings();
    void SaveSettings(SyncSettings syncSettings);
}
