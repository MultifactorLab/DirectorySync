using System.DirectoryServices;
using DirectorySync.Domain;

namespace DirectorySync.Infrastructure.Integrations.Ldap.Windows.Extensions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
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

    public static string GetSingleAttributeValue(this DirectoryEntry entry, LdapAttributeName name)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(name);

        if (!entry.Properties.Contains(name))
        {
            throw new InvalidOperationException($"Entry '{entry}' does not has property '{name}'");
        }

        var values = entry.Properties[name];
        if (values.Count == 0)
        {
            throw new InvalidOperationException($"Entry '{entry}' does not has any property '{name}' values");
        }

        if (values.Count != 1)
        {
            throw new InvalidOperationException($"Entry '{entry}' has more than one '{name}' property values");
        }

        return values[0]?.ToString() ?? throw new InvalidOperationException($"Property '{name}' value of entry '{entry}' is null");
    }
}
