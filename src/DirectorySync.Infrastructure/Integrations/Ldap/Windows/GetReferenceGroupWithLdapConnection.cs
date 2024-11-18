using System.DirectoryServices.Protocols;
using System.Net;
using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap;
using SearchOption = System.DirectoryServices.Protocols.SearchOption;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

internal class GetReferenceGroupWithLdapConnection : IGetReferenceGroup
{
    private readonly LdapOptions _ldapOptions;
    private readonly LdapConnectionString _connectionString;

    public GetReferenceGroupWithLdapConnection(IOptions<LdapOptions> ldapOptions)
    {
        _ldapOptions = ldapOptions.Value;
        _connectionString = new LdapConnectionString(ldapOptions.Value.Path);
    }

    public ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var connection = GetConnection();

        var groupDn = FindGroupDn(guid, connection);
        if (groupDn is null)
        {
            throw new GroupNotFoundException($"Group with GUID '{guid}' was not found");
        }
        var members = GetMembers(groupDn, requiredAttributes, connection);
        return new ReferenceDirectoryGroup(guid, members);

    }

    private LdapConnection GetConnection()
    {
        var id = new LdapDirectoryIdentifier(_connectionString.Host, _connectionString.Port);

        var connenction = new LdapConnection(id, 
            new NetworkCredential(_ldapOptions.Username, _ldapOptions.Password), 
            AuthType.Basic);

        connenction.SessionOptions.ProtocolVersion = 3;
        connenction.SessionOptions.VerifyServerCertificate = (connection, certificate) => true;

        connenction.Bind();
        return connenction;
    }

    private string? FindGroupDn(DirectoryGuid guid, LdapConnection conn)
    {
        var filter = $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";

        var result = Find(filter, ["distinguishedName"], conn);
        var first = result.FirstOrDefault();
        if (first is null)
        {
            return default;
        }

        return first.DistinguishedName;
    }

    private IEnumerable<ReferenceDirectoryUser> GetMembers(string groupDn, 
        string[] requiredAttributes, 
        LdapConnection conn)
    {
        var filter = $"(&(objectClass=user)(memberof={groupDn}))";
        var attrs = requiredAttributes.Concat(["ObjectGUID"]).ToArray();

        var result = Find(filter, attrs, conn);
        foreach (var entry in result)
        {
            var guid = GetObjectGuid(entry);
            var map = requiredAttributes.Select(x => GetAttr(entry, x));
            var attributes = new LdapAttributeCollection(map);
            yield return new ReferenceDirectoryUser(guid, attributes);
        }
    }

    private static DirectoryGuid GetObjectGuid(SearchResultEntry entry)
    {
        var value = entry.Attributes["objectGuid"]?[0];
        if (value is not byte[] bytes)
        {
            throw new InvalidOperationException("Empty GUID");
        }

        return new Guid(bytes);
    }

    private static LdapAttribute GetAttr(SearchResultEntry entry, string attr)
    {
        var value = entry.Attributes[attr]?[0]?.ToString();
        return new LdapAttribute(attr, value);
    }

    private IEnumerable<SearchResultEntry> Find(string filter, 
        string[] requiredAttributes, 
        LdapConnection conn)
    {
        var searchRequest = new SearchRequest(_connectionString.Container,
            filter,
            SearchScope.Subtree,
            requiredAttributes);

        const int pageSize = 500;
        var pageRequestControl = new PageResultRequestControl(pageSize);
        var searchOptControl = new SearchOptionsControl(SearchOption.DomainScope);

        searchRequest.Controls.Add(pageRequestControl);
        searchRequest.Controls.Add(searchOptControl);

        var searchResults = new List<SearchResponse>();
        var pages = 0;

        while (true)
        {
            ++pages;

            var response = conn.SendRequest(searchRequest);
            if (response is not SearchResponse searchResponse)
            {
                throw new Exception($"Invalid search response: {response}");
            }

            searchResults.Add(searchResponse);

            var control = searchResponse.Controls
                .OfType<PageResultResponseControl>()
                .FirstOrDefault();
            if (control != null)
            {
                pageRequestControl.Cookie = control.Cookie;
            }

            if (pageRequestControl.Cookie.Length == 0)
            {
                break;
            }
        }

        foreach (var sr in searchResults)
        {
            foreach (SearchResultEntry entry in sr.Entries)
            {
                yield return entry;
            }
        }
    }
}
