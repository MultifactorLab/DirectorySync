using System.DirectoryServices.Protocols;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal sealed class GetRefUserWithDirectoryServicesProtocols : IGetReferenceUser
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapOptions _ldapOptions;
    private readonly BaseDnResolver _baseDnResolver;
    private readonly ILogger<GetRefGroupWithDirectoryServicesProtocols> _logger;

    public GetRefUserWithDirectoryServicesProtocols(LdapConnectionFactory connectionFactory,
        IOptions<LdapOptions> ldapOptions,
        BaseDnResolver baseDnResolver,
        ILogger<GetRefGroupWithDirectoryServicesProtocols> logger)
    {
        _ldapOptions = ldapOptions.Value;
        _connectionFactory = connectionFactory;
        _baseDnResolver = baseDnResolver;
        _logger = logger;
    }
    public ReferenceDirectoryUser? Execute(DirectoryGuid guid, string[] requiredAttributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var connection = _connectionFactory.CreateConnection();

        var filter = LdapFilters.FindEntryByGuid(guid);
        _logger.LogDebug("Searching by entry with filter '{Filter:s}'...", filter);
        var attrs = requiredAttributes.Concat(["ObjectGUID"]).ToArray();

        var result = Find(filter, attrs, connection);

        var entry = result.FirstOrDefault();

        if (entry != null)
        {
            var map = requiredAttributes.Select(entry.GetFirstValueAttribute);
            var attributes = new LdapAttributeCollection(map);
            return new ReferenceDirectoryUser(GetObjectGuid(entry), attributes);
        }

        return null;
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
        var searchOptControl = new SearchOptionsControl(System.DirectoryServices.Protocols.SearchOption.DomainScope);

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
