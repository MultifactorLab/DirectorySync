namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;

internal sealed class LdapUsernameChanger
{
    public static string ChangeDomain(string username, 
        string newDomain, 
        LdapIdentityFormat ldapIdentityFormat)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username must be provided.");
        }

        if (string.IsNullOrWhiteSpace(newDomain))
        {
            throw new ArgumentException("Domain must be provided.");
        }

        switch (ldapIdentityFormat)
        {
            case LdapIdentityFormat.UserPrincipalName:
                var userPart = username.Split('@')[0];
                return $"{userPart}@{newDomain}";
            
            case LdapIdentityFormat.DomainUsername:
                var usernamePart = username.Split('\\')[1];
                return $"{newDomain}\\{usernamePart}";
            
            case LdapIdentityFormat.SamAccountName:
                return $"{newDomain}\\{username}";
            
            case LdapIdentityFormat.DistinguishedName:
                return ReplaceBaseDn(username, newDomain);
            
            default:
                throw new InvalidOperationException("Unknown username type. Cannot change domain.");
        }
    }

    private static string ReplaceBaseDn(string distinguishedName, string newBaseDn)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName) || string.IsNullOrWhiteSpace(newBaseDn))
        {
            throw new ArgumentException("distinguishedName and newBaseDn must be provided.");
        }
        
        if (!newBaseDn.Contains("DC=", StringComparison.OrdinalIgnoreCase))
        {
            newBaseDn = ConvertDomainToBaseDn(newBaseDn);
        }
        
        var parts = distinguishedName.Split(',');
        
        int firstDcIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].TrimStart().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            {
                firstDcIndex = i;
                break;
            }
        }

        if (firstDcIndex == -1)
        {
            throw new InvalidOperationException("DN does not contain DC components. Cannot replace baseDn.");
        }
        
        var newDn = string.Join(",", parts[..firstDcIndex]) + "," + newBaseDn;
        
        return newDn.TrimStart(',');
    }
    
    private static string ConvertDomainToBaseDn(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("Domain must be provided.");
        }

        var parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = $"DC={parts[i]}";
        }

        return string.Join(",", parts);
    }
}
