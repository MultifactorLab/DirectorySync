using System.DirectoryServices.Protocols;
using DirectorySync.Domain;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static class SearchResultEntryExtensions
{
    /// <summary>
    /// Returns a <see cref="LdapAttribute"/> with empty or single value.
    /// </summary>
    /// <param name="entry">Search Result entry</param>
    /// <param name="attr">Attribute name (type).</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">If <paramref name="entry"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="attr"/> is empty.</exception>
    public static LdapAttribute GetSingleValuedAttribute(this SearchResultEntry entry, string attr)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(attr))
        {
            throw new ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
        }

        var value = entry.Attributes[attr]?[0]?.ToString();
        return new LdapAttribute(attr, value);
    }
}
