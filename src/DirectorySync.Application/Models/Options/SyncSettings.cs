namespace DirectorySync.Application.Models.Options;

public class SyncSettings
{
    public bool Enabled { get; set; }
    public TimeSpan CloudConfigRefreshTimer { get; set; }
    public TimeSpan SyncTimer { get; set; }
    public TimeSpan ScanTimer { get; set; }
    public TimeSpan TimeoutAd { get; set; }

    public GroupMapping[] DirectoryGroupMappings { get; set; } = [];
    public bool IncludeNestedGroups { get; set; } = true;

    public PropsMapping PropertyMapping { get; set; } = new PropsMapping();
}
