namespace DirectorySync.Infrastructure.Adapters.Ldap.Options;

internal sealed class LdapRequestOptions
{
    public bool IncludeNestedGroups { get; set; } = false;
}
