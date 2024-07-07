using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using DirectorySync.Application.Integrations.Ldap.Extensions;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

public class GetReferenceGroupByGuid
{
    private readonly LdapOptions _ldapOptions;
    
    public GetReferenceGroupByGuid(IOptions<LdapOptions> ldapOptions)
    {
        _ldapOptions = ldapOptions.Value;
    }
    
    public ReferenceDirectoryGroup Execute(DirectoryGuid guid, IEnumerable<string> requiredAttributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var ctx = new PrincipalContext(ContextType.Domain,
            _ldapOptions.Name,
            _ldapOptions.Container,
            _ldapOptions.Username,
            _ldapOptions.Password);
        
        using var group = GroupPrincipal.FindByIdentity(ctx, IdentityType.Guid, guid);
        using var members = group.GetMembers(true);
        
        var users = members.OfType<UserPrincipal>().ToArray();
        var withNoGuid = users.Where(x => !x.Guid.HasValue);
        
        var attrs = requiredAttributes.ToArray();
        if (attrs.Length == 0)
        {
            var mapped = users.Except(withNoGuid).Select(x => new ReferenceDirectoryGroupMember(x.Guid!.Value, []));
            return new ReferenceDirectoryGroup(guid, mapped);
        }
        
        var mappedMembers = users.Except(withNoGuid)
            .Select(x => x.GetUnderlyingObject())
            .Cast<DirectoryEntry>()
            .Select(x => Select(x, attrs));

        return new ReferenceDirectoryGroup(guid, mappedMembers);
    }

    private static ReferenceDirectoryGroupMember Select(DirectoryEntry entry, string[] attrs)
    {
        var attributes = attrs.Select(attribute => new LdapAttribute(attribute, entry.GetFirstOrDefaultAttributeValue(attribute)));
#if DEBUG
        _ = new DebugDirectoryAttributes(entry);
#endif
        return new ReferenceDirectoryGroupMember(entry.Guid, attributes);
    }
}
