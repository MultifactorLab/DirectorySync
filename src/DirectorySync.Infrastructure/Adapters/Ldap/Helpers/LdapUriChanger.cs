namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

public static class LdapUriChanger
{
    public static string ReplaceHostInLdapUrl(string ldapUrl, string newHost)
    {
        if (string.IsNullOrWhiteSpace(ldapUrl) || string.IsNullOrWhiteSpace(newHost))
        {
            throw new ArgumentException("ldapUrl and newHost must be provided.");
        }
        
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
