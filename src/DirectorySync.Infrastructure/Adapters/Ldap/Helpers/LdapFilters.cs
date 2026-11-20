using DirectorySync.Application.Models.ValueObjects;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapFilters
{
    public static string FindContainerByGuid(DirectoryGuid guid, ILdapSchema schema)  =>
        schema.LdapServerImplementation switch
        {
            LdapImplementation.OpenLDAP => 
                $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(entryUUID={guid.OctetString}))",
            LdapImplementation.FreeIPA => 
                $"(&(|({schema.ObjectClass}={schema.GroupObjectClass})({schema.ObjectClass}={schema.OrganizationalUnitObjectClass}))(ipaUniqueID={guid.Value.ToString()}))",
            
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

    public static string FindGroupMembersByGroupDn(string groupDn, ILdapSchema schema) =>
        $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberOf={groupDn}))";

    public static string FindEnabledGroupMembersByGroupDn(string groupDn, ILdapSchema schema) => schema.LdapServerImplementation switch
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        // Active Directory only.
        LdapImplementation.ActiveDirectory or LdapImplementation.Samba or LdapImplementation.MultiDirectory  =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberof={groupDn})(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
        
        // Для FreeIPA, ALD Pro, 389DS: ipa user-disable => nsAccountLock=true
        // memberOf-плагин в FreeIPA включён всегда
        LdapImplementation.OpenLDAP or LdapImplementation.FreeIPA =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberOf={groupDn})(!(nsAccountLock=TRUE)))",

        _ => $"(&({schema.ObjectClass}={schema.UserObjectClass})(memberOf={groupDn}))"
    };

    public static string FindEnabledUsersInOu(ILdapSchema schema)=> schema.LdapServerImplementation switch
    {
        LdapImplementation.ActiveDirectory or LdapImplementation.Samba or LdapImplementation.MultiDirectory =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
        LdapImplementation.OpenLDAP or LdapImplementation.FreeIPA =>
            $"(&({schema.ObjectClass}={schema.UserObjectClass})(!(nsAccountLock=TRUE)))",
        
        _ => $"({schema.ObjectClass}={schema.UserObjectClass})"
    };

    public static string FindEntriesByGuids(IEnumerable<DirectoryGuid> guids, ILdapSchema schema) =>
        schema.LdapServerImplementation switch
        {
            LdapImplementation.FreeIPA => BuildOrFilter(guids, "ipaUniqueID", v => v.Value.ToString()),
            LdapImplementation.OpenLDAP => BuildOrFilter(guids, "entryUUID", v => v.OctetString),
            _ => BuildOrFilter(guids, "objectGUID", v => v.OctetString)
        };
    
    public static string GetIdentifierAttribute(ILdapSchema schema) =>
        schema.LdapServerImplementation switch
        {
            LdapImplementation.FreeIPA  => "ipaUniqueID",
            LdapImplementation.OpenLDAP => "entryUUID",
            _                            => "objectGuid"
        };

    private static string BuildOrFilter(IEnumerable<DirectoryGuid> guids, string attr, Func<DirectoryGuid, string> format)
    {
        var filters = guids.Select(g => $"({attr}={format(g)})").ToArray();
        return filters.Length == 1 ? filters[0] : $"(|{string.Join(string.Empty, filters)})";
    }
}

