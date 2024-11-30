using DirectorySync.Domain;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal static partial class LdapFilters
{
    public static string FindGroupByGuid(DirectoryGuid guid)
    {
        return $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
    }    
    
    public static string FindEnabledGroupMembersByGroupDn(string groupDn)
    {
        return $"(&(objectClass=user)(memberof={groupDn}))";
    }
}
