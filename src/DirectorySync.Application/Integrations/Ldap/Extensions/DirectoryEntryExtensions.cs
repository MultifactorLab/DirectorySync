using System.DirectoryServices;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Ldap.Extensions;

internal static class DirectoryEntryExtensions
{
    public static string? GetFirstOrDefaultAttributeValue(this DirectoryEntry entry, LdapAttributeName name)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(name);

        return !entry.Properties.Contains(name) 
            ? default 
            : entry.Properties[name][0]?.ToString();
    }
}
