using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static class LdapFilters
{
    public static string FindGroupByGuid(DirectoryGuid guid)
    {
        return $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
    }    
    
    public static string FindEnabledGroupMembersByGroupDn(string groupDn)
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        return $"(&(objectClass=user)(memberof={groupDn})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
    }

    public static string FindEnabledGroupMembersByGroupDnRecursively(string groupDn)
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        return $"(&(objectClass=user)(memberof:1.2.840.113556.1.4.1941:={groupDn})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
    }

    public static string FindEntryByGuid(DirectoryGuid guid)
    {
        return $"(objectGUID={guid.OctetString})";
    }
    
    public static string FindEntriesByGuids(IEnumerable<DirectoryGuid> guids)
    {
        var filters = guids
            .Select(guid => $"(objectGUID={guid.OctetString})")
            .ToArray();

        if (filters.Length == 1)
        {
            return filters[0];
        }

        return $"(|{string.Join(string.Empty, filters)})";
    }
}

