namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapUriChanger
{
    public static string ReplaceHostInLdapUrl(string ldapUrl, string newHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ldapUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(newHost);
        
        if (!Uri.TryCreate(ldapUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid LDAP URL format: {ldapUrl}");
        }
        
        var builder = new UriBuilder(uri)
        {
            Host = newHost
        };

        return builder.Uri.ToString();
    }  
}
