using DirectorySync.Application.Models.ValueObjects;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static class LdapFilters
{
    public static string FindContainerByGuid(DirectoryGuid guid, ILdapSchema schema)
    {
        return $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(objectGUID={guid.OctetString}))";
    }

    public static string FindGroup(ILdapSchema schema)
    {
        return $"(&({schema.ObjectClass}={schema.GroupObjectClass}))";
    }
    
    public static string FindOu(ILdapSchema schema)
    {
        return $"(&({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))";
    }

    public static string FindEnabledGroupMembersByGroupDn(string groupDn, ILdapSchema schema)
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        return $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberof={groupDn})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
    }

    public static string FindEnabledGroupMembersByGroupDnRecursively(string groupDn, ILdapSchema schema)
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        return $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberof:1.2.840.113556.1.4.1941:={groupDn})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
    }

    public static string FindEnabledUsersInOu(ILdapSchema schema)
    {
        return $"(&({schema.ObjectClass}={schema.UserObjectClass})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
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

