using System.DirectoryServices;
using DirectorySync.Application.Exceptions;
using DirectorySync.Domain;
using DirectorySync.Domain.Abstractions;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Integrations.Ldap.Windows.Extensions;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
internal class GetReferenceGroupWithDirectorySearcher : IGetReferenceGroup
{
    private readonly LdapOptions _ldapOptions;

    public GetReferenceGroupWithDirectorySearcher(IOptions<LdapOptions> ldapOptions)
    {
        _ldapOptions = ldapOptions.Value;
    }
    
    public ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes)
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
        
        return new DirectoryEntry($"LDAP://{path}", _ldapOptions.Username, _ldapOptions.Password);
    }

    private static string? GetGroupDn(DirectoryGuid guid, DirectoryEntry root)
    {
        using var searcher = new DirectorySearcher(root);
        searcher.SearchScope = SearchScope.Subtree;
        searcher.Filter = $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
        
        searcher.PropertiesToLoad.Clear();
        searcher.PropertiesToLoad.AddRange(["ObjectGUID", "distinguishedName"]);
        
        var result = searcher.FindOne();
        return result?.GetDirectoryEntry().GetSingleAttributeValue("distinguishedName");
    }

    private IEnumerable<ReferenceDirectoryUser> GetMembers(string groupDn, string[] requiredAttributes, DirectoryEntry root)
    {
        using var searcher = new DirectorySearcher(root);
        searcher.SearchScope = SearchScope.Subtree;
        searcher.Filter = $"(&(objectClass=user)(memberof={groupDn}))";
        searcher.PageSize = _ldapOptions.PageSize;
        
        searcher.PropertiesToLoad.Clear();
        var props = requiredAttributes
            .Concat(["ObjectGUID", "sAMAccountName", "distinguishedName"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        searcher.PropertiesToLoad.AddRange(props);
        
        using var collection = searcher.FindAll();
        foreach (SearchResult searchResult in collection)
        {
            var guid = searchResult.GetObjectGuid();
            var attributes = requiredAttributes.Select(x => new LdapAttribute(x, searchResult.GetString(x)));
            var attrCollection = new LdapAttributeCollection(attributes);
            yield return new ReferenceDirectoryUser(guid, attrCollection);
        }
    }
}
