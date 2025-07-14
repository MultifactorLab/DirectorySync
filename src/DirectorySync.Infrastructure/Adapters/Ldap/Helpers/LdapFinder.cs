using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap.Connection;
using System.DirectoryServices.Protocols;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal sealed class LdapFinder
{
    private readonly LdapOptions _ldapOptions;

    public LdapFinder(IOptions<LdapOptions> ldapOptions)
    {
        _ldapOptions = ldapOptions.Value;
    }

    public IEnumerable<SearchResultEntry> Find(string filter,
        string[] requiredAttributes,
        string namingContext,
        ILdapConnection conn)
    {
        var searchRequest = new SearchRequest(namingContext,
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
