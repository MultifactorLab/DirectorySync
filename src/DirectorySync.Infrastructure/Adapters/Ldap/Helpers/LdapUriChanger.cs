using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapUriChanger
{
    public static string ReplaceHostInLdapUrl(string ldapUrl, LdapDomain newHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ldapUrl);
        ArgumentNullException.ThrowIfNull(newHost);
        
        if (!Uri.TryCreate(ldapUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid LDAP URL format: {ldapUrl}");
        }
        
        var builder = new UriBuilder(uri)
        {
            Host = newHost.Value
        };

        return builder.Uri.ToString();
    }  
}
