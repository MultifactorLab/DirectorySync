using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;

internal static class SearchResultEntryExtensions
{
    public static string? GetAttributeValue(this SearchResultEntry entry, string attributeName)
    {
        ArgumentNullException.ThrowIfNull(entry);
        
        if (!entry.Attributes.Contains(attributeName))
        {
            return null;
        }

        DirectoryAttribute attribute = entry.Attributes[attributeName];
        return attribute.Count > 0 ? attribute[0].ToString() : null;
    }
    
    /// <summary>
    /// Returns a <see cref="LdapAttribute"/> with empty or single value.
    /// </summary>
    /// <param name="entry">Search Result entry</param>
    /// <param name="attr">Attribute name (type).</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">If <paramref name="entry"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="attr"/> is empty.</exception>
    public static LdapAttribute GetFirstValueAttribute(this SearchResultEntry entry, string attr)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(attr))
        {
            throw new ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
        }

        var value = entry.Attributes[attr]?.GetValues(typeof(string)).FirstOrDefault()?.ToString();
        return new LdapAttribute(attr, value);
    }
}
