using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Services;

internal class SyncOptions
{
    public bool Enabled { get; set; }
    public TimeSpan SyncTimer { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan NewUserHandleTimer { get; set; } = TimeSpan.FromMinutes(15);

    public Guid[] Groups { get; init; } = [];
    
    public MultifactorGroupPolicyPreset MultifactorGroupPolicyPreset { get; set; }
}

internal class MultifactorGroupPolicyPreset
{
    public string SignUpGroups { get; set; }
}