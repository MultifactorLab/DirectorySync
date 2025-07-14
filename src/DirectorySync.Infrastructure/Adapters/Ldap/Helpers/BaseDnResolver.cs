using System.DirectoryServices.Protocols;
using DirectorySync.Infrastructure.Integrations.Ldap;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal sealed class BaseDnResolver
{
    const string _defaultNamingContextAttr = "defaultNamingContext";

    private readonly LdapConnectionFactory _connectionFactory;
    private readonly ILogger<BaseDnResolver> _logger;
    // dictionary - локальный кэш домен - baseDn

    public BaseDnResolver(LdapConnectionFactory connectionFactory,
        ILogger<BaseDnResolver> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns a Base DN from the LDAP connection string if presented. Otherwise, connects to a LDAP server and consumes Base DN from the RootDSE.
    /// </summary>
    /// <returns>BASE DN.</returns>
    public string GetBaseDn(LdapConnectionOptions options)
    {
        using var connection = _connectionFactory.CreateConnection(options);

        var dn = GetBaseDnInternal(connection, options.ConnectionString);
        return dn;
    }

    private string GetBaseDnInternal(ILdapConnection conn, LdapConnectionString connectionString)
    {
        if (connectionString.HasBaseDn)
        {
            _logger.LogDebug("Base DN was consumed from config: {BaseDN:l}", connectionString.Container);
            return connectionString.Container;
        }

        _logger.LogDebug("Try to consume Base DN from LDAP server");

        var filter = "(objectclass=*)";
        var searchRequest = new SearchRequest(null, filter, SearchScope.Base, "*");

        var response = conn.SendRequest(searchRequest);
        if (response is not SearchResponse searchResponse)
        {
            throw new Exception($"Invalid search response: {response}");
        }

        if (searchResponse.Entries.Count == 0)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: empty search result entrues");
        }

        var defaultNamingContext = searchResponse.Entries[0].GetFirstValueAttribute(_defaultNamingContextAttr);
        if (!defaultNamingContext.HasValues)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: '{_defaultNamingContextAttr}' attr was not found");
        }

        var value = defaultNamingContext.GetNotEmptyValues().FirstOrDefault();
        if (value is null)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: '{_defaultNamingContextAttr} attr value is empty'");
        }

        _logger.LogDebug("Base DN was consumed from LDAP server: {BaseDN:l}", value);

        return value;
    }
}
