using System.DirectoryServices.Protocols;

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
}
