using System.DirectoryServices;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

public class DebugDirectoryAttributes
{
    public LdapAttribute[] Attributes { get; }
    
    public DebugDirectoryAttributes(DirectoryEntry entry)
    {
        var attrs = new List<LdapAttribute>();
        foreach (var name in entry.Properties.PropertyNames)
        {
            var prop = entry.Properties[(string)name];
            
            var values = new List<string?>();
            foreach (var val in prop)
            {
                values.Add(val?.ToString());
            }
            attrs.Add(new LdapAttribute(new LdapAttributeName(prop.PropertyName), values));
        }

        Attributes = attrs.ToArray();
    }
}
