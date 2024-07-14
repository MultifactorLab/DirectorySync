using System.DirectoryServices;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Ldap.Extensions;

internal static class SearchResultExtensions
{
    public static DirectoryGuid GetObjectGuid(this SearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var value = result.Properties["objectGuid"][0];
        if (value is not byte[] bytes)
        {
            throw new InvalidOperationException("Empty GUID");
        }

        return new Guid(bytes);
    }
    
    public static string? GetString(this SearchResult result, string property)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(property));
        }

        var values = result.Properties[property];
        return values.Count == 0 
            ? null 
            : values[0].ToString();
    }
}
