using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.GetSettings.Dto;

public class PropsMappingDto
{
    public string? IdentityAttribute { get; init; }

    public ReadOnlyDictionary<string, string> AdditionalAttributes { get; init; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    public string? NameAttribute { get; init; }
    public string[] EmailAttributes { get; init; } = [];
    public string[] PhoneAttributes { get; init; } = [];
}
