using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Services;

internal class SyncOptions
{
    public TimeSpan SyncTimer { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan NewUserHandleTimer { get; set; } = TimeSpan.FromMinutes(15);

    public Guid[] Groups { get; init; } = [];
    
    public MultifactorGroupPolicyPreset MultifactorGroupPolicyPreset { get; set; }

    public bool SyncEnabled => SyncTimer > TimeSpan.Zero && SyncTimer < TimeSpan.MaxValue;
    public bool NewUserHandleEnabled => NewUserHandleTimer > TimeSpan.Zero && NewUserHandleTimer < TimeSpan.MaxValue;
}

internal class MultifactorGroupPolicyPreset
{
    public string SignUpGroups { get; set; }
}
