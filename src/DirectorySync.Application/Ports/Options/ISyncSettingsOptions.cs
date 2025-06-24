using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;

namespace DirectorySync.Application.Ports.Options;

public interface ISyncSettingsOptions
{
    SyncSettings Current { get; }
    string[] GetRequiredAttributeNames();
}
