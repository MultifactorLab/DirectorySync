namespace DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;

public class PropsMappingDto
{
    public string? IdentityAttribute { get; init; }
    public string? NameAttribute { get; init; }
    public string[] EmailAttributes { get; init; } = [];
    public string[] PhoneAttributes { get; init; } = [];
}
