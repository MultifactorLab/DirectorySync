using DirectorySync.Application.Models.Options;

namespace DirectorySync.Application.Ports.Options;

public interface ISyncSettingsOptions
{
    SyncSettings Current { get; }
    
    IDisposable OnChange(Action<SyncSettings> action);
    string[] GetRequiredAttributeNames();
}
