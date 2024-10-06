namespace DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;

public class CloudConfigDto
{
    public bool Enabled { get; init; }

    public string[] DirectoryGroups { get; init; } = [];

    public PropsMappingDto PropertyMapping { get; init; } = new();
        
    public MultifactorGroupPolicyPresetDto MultifactorGroupPolicyPreset { get; init; } = new();
        
}
