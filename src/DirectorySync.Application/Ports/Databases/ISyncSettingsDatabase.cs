using DirectorySync.Application.Models.Core;

namespace DirectorySync.Application.Ports.Databases;

public interface ISyncSettingsDatabase
{
    SyncSettings? GetSyncSettings();
    void SaveSettings(SyncSettings syncSettings);
}
