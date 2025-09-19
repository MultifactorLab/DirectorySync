using DirectorySync.Application.Models.ValueObjects;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static class LdapFilters
{
    public static string FindContainerByGuid(DirectoryGuid guid, ILdapSchema schema)  =>
        schema.LdapServerImplementation switch
        {
            LdapImplementation.OpenLDAP => 
                $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(entryUUID={guid.OctetString}))",
            LdapImplementation.FreeIPA => 
                $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(ipaUniqueID={guid.OctetString}))",
            
            _ => $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(objectGUID={guid.OctetString}))"
        };
    
    public static string FindOuNestContainers(ILdapSchema schema)
    {
        return $"(|(objectClass={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))";
    }
    
    public static string FindGroupByDn(string dn, ILdapSchema schema)
    {
        return $"(&(objectClass={schema.GroupObjectClass})({schema.Dn}={dn}))";
    }

    public static string FindGroupMembersByGroupDn(string groupDn, ILdapSchema schema) => schema.LdapServerImplementation switch
    {
        LdapImplementation.OpenLDAP or LdapImplementation.FreeIPA =>
            $"(&({schema.ObjectClass}={schema.GroupObjectClass})({schema.Dn}={groupDn}))",
        
        _ => $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberOf={groupDn}))"
    };

    public static string FindEnabledGroupMembersByGroupDn(string groupDn, ILdapSchema schema) => schema.LdapServerImplementation switch
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        LdapImplementation.ActiveDirectory or LdapImplementation.Samba or LdapImplementation.MultiDirectory  =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberof={groupDn})(!(userAccountControl:1.2.840.113556.1.4.803:=2))",
        
        _ => $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberof={groupDn}))"
    };

    public static string FindEnabledUsersInOu(ILdapSchema schema)=> schema.LdapServerImplementation switch
    {
        LdapImplementation.ActiveDirectory or LdapImplementation.Samba or LdapImplementation.MultiDirectory =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
        LdapImplementation.OpenLDAP or LdapImplementation.FreeIPA =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(!(nsAccountLock=TRUE)))",
        
        _ => $"({schema.ObjectClass}={schema.UserObjectClass})"
    };

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

