namespace DirectorySync.Services;

internal class SyncOptions
{
    public bool Enabled { get; set; }
    public TimeSpan SyncTimer { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan ScanTimer { get; set; } = TimeSpan.FromMinutes(15);

    public Guid[] Groups { get; init; } = [];

    public MultifactorGroupPolicyPreset MultifactorGroupPolicyPreset { get; set; } = new ();

    public bool SyncEnabled => Enabled && SyncTimer > TimeSpan.Zero && SyncTimer < TimeSpan.MaxValue;
    public bool ScanEnabled => Enabled && ScanTimer > TimeSpan.Zero && ScanTimer < TimeSpan.MaxValue;
}

internal class MultifactorGroupPolicyPreset
{
    public string[] SignUpGroups { get; set; } = [];
}
