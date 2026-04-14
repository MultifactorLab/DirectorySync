namespace DirectorySync.Infrastructure.Adapters.Ldap.Options;

public class LdapAttributeMappingOptions
{
    public string IdentityAttribute { get; set; } = string.Empty;

    public string? NameAttribute { get; set; }

    public string[] EmailAttributes { get; set; } = [];

    public string[] PhoneAttributes { get; set; } = [];
}
