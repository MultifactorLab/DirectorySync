﻿using DirectorySync.Domain;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static partial class LdapFilters
{
    public static string FindGroupByGuid(DirectoryGuid guid)
    {
        return $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
    }    
    
    public static string FindEnabledGroupMembersByGroupDn(string groupDn)
    {
        // NOT disabled: UAC flags does not contain UF_ACCOUNT_DISABLE
        return $"(&(objectClass=user)(memberof={groupDn})(!userAccountControl:1.2.840.113556.1.4.803:=2))";
    }
}
