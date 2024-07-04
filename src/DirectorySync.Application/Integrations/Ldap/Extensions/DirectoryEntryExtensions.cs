using System.DirectoryServices;

namespace DirectorySync.Application.Integrations.Ldap.Extensions;

internal static class DirectoryEntryExtensions
{
    public static string? GetFirstOrDefaultAttributeValue(this DirectoryEntry entry, string name)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        if (!entry.Properties.Contains(name))
        {
            return default;
        }

        return entry.Properties[name][0]?.ToString();
    }
}