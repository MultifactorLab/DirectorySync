using DirectorySync.Application.Models.Core;

namespace DirectorySync.Application.Ports.Options;

public interface ISyncSettingsOptions
{
    SyncSettings? Current { get; }
    string[] GetRequiredAttributeNames();
}
