using System.DirectoryServices;
using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Integrations.Ldap.Extensions;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

internal class GetReferenceGroupWithDirectorySearcher : IGetReferenceGroup
{
    private readonly LdapOptions _ldapOptions;

    public GetReferenceGroupWithDirectorySearcher(IOptions<LdapOptions> ldapOptions)
    {
        _ldapOptions = ldapOptions.Value;
    }
    
    public ReferenceDirectoryGroup Execute(DirectoryGuid guid, IEnumerable<string> requiredAttributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var root = GetRoot();

        var groupDn = GetGroupDn(guid, root);
        if (groupDn is null)
        {
            throw new GroupNotFoundException($"Group with GUID '{guid}' was not found");
        }

        var members = GetMembers(groupDn, requiredAttributes, root);
        return new ReferenceDirectoryGroup(guid, members);
    }

    private DirectoryEntry GetRoot()
    {
        var path = _ldapOptions.Path;
        if (!string.IsNullOrWhiteSpace(_ldapOptions.Container))
        {
            path += $"/{_ldapOptions.Container}";
        }
        
        return new DirectoryEntry($"LDAP://{path}", 
            _ldapOptions.Username, 
            _ldapOptions.Password);
    }

    private static string? GetGroupDn(DirectoryGuid guid, DirectoryEntry root)
    {
        using var searcher = new DirectorySearcher(root);
        searcher.SearchScope = SearchScope.Subtree;
        searcher.Filter = $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
        
        searcher.PropertiesToLoad.Clear();
        searcher.PropertiesToLoad.AddRange(["ObjectGUID", "sAMAccountName", "distinguishedName"]);
        
        var result = searcher.FindOne();
        return result?.GetDirectoryEntry().GetSingleAttributeValue("distinguishedName");
    }

    private static IEnumerable<ReferenceDirectoryGroupMember> GetMembers(string groupDn, IEnumerable<string> requiredAttributes, DirectoryEntry root)
    {
        using var searcher = new DirectorySearcher(root);
        searcher.SearchScope = SearchScope.Subtree;
        searcher.Filter = $"(&(objectClass=user)(memberof={groupDn}))";
        searcher.PageSize = 500;
        
        searcher.PropertiesToLoad.Clear();
        var attrs = requiredAttributes.ToArray();
        var props = attrs
            .Concat(["ObjectGUID", "sAMAccountName", "distinguishedName"])
            .Distinct()
            .ToArray();
        searcher.PropertiesToLoad.AddRange(props);
        
        using var collection = searcher.FindAll();
        foreach (SearchResult searchResult in collection)
        {
            var guid = searchResult.GetObjectGuid();
            var attributes = attrs.Select(x => new LdapAttribute(x, searchResult.GetString(x)));
            yield return new ReferenceDirectoryGroupMember(guid, attributes);
        }
    }
}
