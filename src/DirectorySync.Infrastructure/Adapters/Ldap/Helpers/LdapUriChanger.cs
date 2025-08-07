using DirectorySync.Application.Models.ValueObjects;
using Multifactor.Core.Ldap;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapUriChanger
{
    public static LdapConnectionString ReplaceHostInLdapConnectionString(LdapConnectionString mainLdapConnectionString, LdapDomain newHost)
    {
        ArgumentNullException.ThrowIfNull(mainLdapConnectionString);
        ArgumentNullException.ThrowIfNull(newHost);

        if (!Uri.TryCreate(mainLdapConnectionString.WellFormedLdapUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid LDAP URL format: {mainLdapConnectionString.WellFormedLdapUrl}");
        }
        
        var builder = new UriBuilder(uri)
        {
            Host = newHost.Value
        };

        return new LdapConnectionString(builder.Uri.ToString());
    }  
}
