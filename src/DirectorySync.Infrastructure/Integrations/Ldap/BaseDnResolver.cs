using System.DirectoryServices.Protocols;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Multifactor.Core.Ldap;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

internal sealed class BaseDnResolver
{
    const string _defaultNamingContextAttr = "defaultNamingContext";

    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapConnectionString _connectionString;
    private readonly ILogger<BaseDnResolver> _logger;
    private readonly Lazy<string> _dn;

    public BaseDnResolver(LdapConnectionFactory connectionFactory,
        LdapConnectionString connectionString,
        ILogger<BaseDnResolver> logger)
    {
        _connectionFactory = connectionFactory;
        _connectionString = connectionString;
        _logger = logger;
        _dn = new Lazy<string>(() =>
        {
            using var conn = _connectionFactory.CreateConnection();
            var dn = GetBaseDnInternal(conn);
            return dn;
        });
    }

    /// <summary>
    /// Returns a Base DN from the LDAP connection string if presented. Otherwise, connects to a LDAP server and consumes Base DN from the RootDSE.
    /// </summary>
    /// <returns>BASE DN.</returns>
    public string GetBaseDn() => _dn.Value;

    private string GetBaseDnInternal(LdapConnection conn)
    {
        if (_connectionString.HasBaseDn)
        {
            _logger.LogDebug("Base DN was consumed from config: {BaseDN:l}", _connectionString.Container);
            return _connectionString.Container;
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
