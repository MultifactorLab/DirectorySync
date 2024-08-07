using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Integrations.Multifactor;

internal class LdapAttributeMappingOptions
{
    [Required]
    public required string IdentityAttribute { get; init; }
    
    public string? NameAttribute { get; init; }

    public string[] EmailAttributes { get; init; } = [];

    public string[] PhoneAttributes { get; init; } = [];
}
