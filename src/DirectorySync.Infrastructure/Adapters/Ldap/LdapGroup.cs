using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchOption = System.DirectoryServices.Protocols.SearchOption;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

internal sealed class LdapGroup : ILdapGroupPort
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapOptions _ldapOptions;
    private readonly LdapRequestOptions _requestOptions;
    private readonly BaseDnResolver _baseDnResolver;
    private readonly ILogger<LdapGroup> _logger;

    public LdapGroup(LdapConnectionFactory connectionFactory,
        IOptions<LdapOptions> ldapOptions,
        IOptions<LdapRequestOptions> requestOptions,
        BaseDnResolver baseDnResolver,
        ILogger<LdapGroup> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapOptions = ldapOptions.Value;
        _requestOptions = requestOptions.Value;
        _baseDnResolver = baseDnResolver;
        _logger = logger;
    }
    
    public GroupModel? GetByGuidAsync(DirectoryGuid objectGuid)
    {
        ArgumentNullException.ThrowIfNull(objectGuid);

        using var connection = _connectionFactory.CreateConnection();

        var groupDn = FindGroupDn(objectGuid, connection);
        if (groupDn is null)
        {
            return null;
        }

        var members = GetMembers(
            groupDn,
            connection
        ).ToList();
        
        return GroupModel.Create(objectGuid, members);
    }

    public ReadOnlyCollection<GroupModel>? GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);

        var guidList = objectGuids.ToList();
        if (guidList.Count == 0)
        {
            return ReadOnlyCollection<GroupModel>.Empty;
        }

        using var connection = _connectionFactory.CreateConnection();

        var result = new List<GroupModel>();

        foreach (var guid in guidList)
        {
            var groupDn = FindGroupDn(guid, connection);
            if (groupDn is null)
            {
                continue;
            }

            var members = GetMembers(
                groupDn,
                connection
            ).ToList();

            result.Add(GroupModel.Create(guid, members));
        }

        return result.AsReadOnly();
    }
    
    private string? FindGroupDn(DirectoryGuid guid, LdapConnection conn)
    {
        var filter = LdapFilters.FindGroupByGuid(guid);
        _logger.LogDebug("Searching by group with filter '{Filter:s}'...", filter);

        var result = Find(filter, ["distinguishedName"], conn);
        var first = result.FirstOrDefault();
        if (first is null)
        {
            return default;
        }

        return first.DistinguishedName;
    }

    private IEnumerable<DirectoryGuid> GetMembers(string groupDn,
        LdapConnection conn)
    {
        var filter = GetFilter(groupDn);
        _logger.LogDebug("Searching by group members with filter '{Filter:s}'...", filter);
        var attrs = new string[] { "ObjectGUID" };

        var result = Find(filter, attrs, conn);
        foreach (var entry in result)
        {
            yield return GetObjectGuid(entry);
        }
    }

    private string GetFilter(string groupDn)
    {
        if (_requestOptions.IncludeNestedGroups)
        {
            return LdapFilters.FindEnabledGroupMembersByGroupDnRecursively(groupDn);
        }

        return LdapFilters.FindEnabledGroupMembersByGroupDn(groupDn);
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

    private IEnumerable<SearchResultEntry> Find(string filter,
        string[] requiredAttributes,
        LdapConnection conn)
    {
        var baseDn = _baseDnResolver.GetBaseDn();
        var searchRequest = new SearchRequest(baseDn,
            filter,
            SearchScope.Subtree,
            requiredAttributes);

        var pageRequestControl = new PageResultRequestControl(_ldapOptions.PageSize);
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
