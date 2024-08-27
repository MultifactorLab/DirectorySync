namespace DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;

public class DirectorySyncSettingsDto
{
    public bool Enabled { get; init; }
    public TimeSpan SyncTimer { get; init; }
    public TimeSpan ScanTimer { get; init; }

    public string[] DirectoryGroups { get; init; } = [];

    public PropsMappingDto PropertyMapping { get; init; } = new();
        
    public MultifactorGroupPolicyPresetDto MultifactorGroupPolicyPreset { get; init; } = new();
        
}
