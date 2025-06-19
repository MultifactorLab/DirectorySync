using DirectorySync.Application.Models.Options;

namespace DirectorySync.Application.Ports.Options;

public interface ISyncSettingsOptions
{
    // DirectorySync.Application.Workloads.RequiredLdapAttributes
    string[] GetRequiredAttributeNames();
    SyncSettings GetSyncSettings();
}
