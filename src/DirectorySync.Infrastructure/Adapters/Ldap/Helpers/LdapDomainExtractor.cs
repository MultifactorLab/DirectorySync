using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

public static class LdapDomainExtractor
{
    public static LdapDomain GetDomainFromDn(string distinguishedName)
    {
        var parts = distinguishedName.Split(',');
        var domainParts = parts
            .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(3))
            .ToList();

        return new LdapDomain(string.Join('.', domainParts));
    }
}
