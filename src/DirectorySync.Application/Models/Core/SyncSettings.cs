using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public class SyncSettings
{
    public bool Enabled { get; set; }
    public TimeSpan CloudConfigRefreshTimer { get; set; }
    public TimeSpan SyncTimer { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan ScanTimer { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan TimeoutAd { get; set; }

    public Guid[] TrackingGroups { get; set; } = [];
    
    public GroupMapping[] DirectoryGroupMappings { get; set; } = [];
    public bool IncludeNestedGroups { get; set; } = true;

    public PropsMapping PropertyMapping { get; set; } = new PropsMapping();
    
    public bool SendEnrollmentLink { get; set; }
    public TimeSpan EnrollmentLinkTtl { get; set; }
    
    public bool SyncEnabled => Enabled && SyncTimer > TimeSpan.Zero && SyncTimer < TimeSpan.MaxValue;
    public bool ScanEnabled => Enabled && ScanTimer > TimeSpan.Zero && ScanTimer < TimeSpan.MaxValue;
    public bool SyncSettingsEnabled => Enabled && CloudConfigRefreshTimer > TimeSpan.Zero && CloudConfigRefreshTimer < TimeSpan.MaxValue;
}
