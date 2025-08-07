namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;

public enum LdapIdentityFormat
{
    None = 0,
    UserPrincipalName = 1,
    UidAndNetbios = 2, // uid@netbios
    SamAccountName = 3,
    DomainUsername = 4, // NETBIOS\uid
    DistinguishedName = 5
}
