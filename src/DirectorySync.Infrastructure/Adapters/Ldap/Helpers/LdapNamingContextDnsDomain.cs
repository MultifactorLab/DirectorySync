using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapNamingContextDnsDomain
{
    /// <summary>
    /// Builds the DNS domain name from an LDAP default naming context (e.g. DC=child,DC=contoso,DC=com to child.contoso.com).
    /// </summary>
    public static LdapDomain FromNamingContext(string namingContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namingContext);

        var parts = new List<string>();
        foreach (var segment in namingContext.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith("DC=", StringComparison.OrdinalIgnoreCase) && segment.Length > 3)
            {
                parts.Add(segment[3..]);
            }
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("Naming context does not contain DC= components.", nameof(namingContext));
        }

        return new LdapDomain(string.Join('.', parts));
    }
}
