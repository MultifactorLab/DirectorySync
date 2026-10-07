using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;

internal static class SearchResultEntryExtensions
{
    // позволяем парсинг гарантировано UUID-атрибутов
    private static readonly HashSet<string> _guidAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "objectGUID",
        "schemaIDGUID",
        "attributeGUID",
        "rightsGUID",
        "msExchMailboxGUID",
        "mS-DS-ConsistencyGuid"
    };
    
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
        
        if (TryParseGuid(entry, attr, out var guid))
        {
            return new LdapAttribute(attr, guid.ToString());
        }
        
        var stringValue = GetFirstValue<string>(entry, attr);
        return new LdapAttribute(attr, stringValue?.ToString());
    }
    
    private static bool TryParseGuid(SearchResultEntry entry, string attr, out Guid guid)
    {
        guid = Guid.Empty;
        
        if (!_guidAttributes.Contains(attr)) { return false; }
        
        var bytes = GetFirstValue<byte[]>(entry, attr);

        if (bytes is not byte[] { Length: 16 } guidBytes) { return false; }

        guid = new Guid(guidBytes);
        return true;
    }

    // T -- либо строка, либо byte[], такие правила AD
    private static object? GetFirstValue<T>(SearchResultEntry entry, string attr) 
        => entry.Attributes[attr]?.GetValues(typeof(T)).FirstOrDefault();
}
