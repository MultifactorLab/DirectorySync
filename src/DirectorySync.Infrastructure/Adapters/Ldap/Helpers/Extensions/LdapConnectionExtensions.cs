using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Connection;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;

internal static class LdapConnectionExtensions
{
    public static SearchResultEntryCollection QueryDomains(this ILdapConnection connection,
        string dn,
        string filter,
        SearchScope scope,
        string[] attributes)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(dn);
        ArgumentException.ThrowIfNullOrEmpty(filter);
        
        var searchRequest = new SearchRequest(dn, filter, scope, attributes);
        var response = (SearchResponse)connection.SendRequest(searchRequest);

        return response.Entries;
    }
}
