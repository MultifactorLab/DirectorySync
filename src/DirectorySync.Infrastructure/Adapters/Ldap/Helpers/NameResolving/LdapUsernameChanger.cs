using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;

internal sealed class LdapUsernameChanger
{
    public static string ChangeDomain(string username, 
        LdapDomain newDomain, 
        LdapIdentityFormat ldapIdentityFormat)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentNullException.ThrowIfNull(newDomain);

        switch (ldapIdentityFormat)
        {
            case LdapIdentityFormat.UserPrincipalName:
                var userPart = username.Split('@')[0];
                return $"{userPart}@{newDomain}";
            
            case LdapIdentityFormat.DomainUsername:
                var usernamePart = username.Split('\\')[1];
                return $"{newDomain}\\{usernamePart}";
            
            case LdapIdentityFormat.SamAccountName:
                return $"{username}";
            
            case LdapIdentityFormat.DistinguishedName:
                return ReplaceBaseDn(username, newDomain);
            
            default:
                throw new InvalidOperationException("Unknown username type. Cannot change domain.");
        }
    }

    private static string ReplaceBaseDn(string distinguishedName, LdapDomain newBaseDn)
    {
        if (!newBaseDn.Value.Contains("DC=", StringComparison.OrdinalIgnoreCase))
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
    
    private static LdapDomain ConvertDomainToBaseDn(LdapDomain domain)
    {
        var parts = domain.Value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = $"DC={parts[i]}";
        }

        return new LdapDomain(string.Join(",", parts));
    }
}
