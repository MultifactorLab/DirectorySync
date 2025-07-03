using System.DirectoryServices.Protocols;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Integrations.Ldap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal sealed class BaseDnResolver
{
    const string _defaultNamingContextAttr = "defaultNamingContext";

    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapOptions _ldapOptions;
    private readonly ILogger<BaseDnResolver> _logger;
    private readonly Lazy<string> _dn;

    public BaseDnResolver(LdapConnectionFactory connectionFactory,
        IOptions<LdapOptions> ldapOptions,
        ILogger<BaseDnResolver> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapOptions = ldapOptions.Value;

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        _logger = logger;
        _dn = new Lazy<string>(() =>
        {
            using var conn = _connectionFactory.CreateConnection(options);
            var dn = GetBaseDnInternal(conn);
            return dn;
        });
    }

    /// <summary>
    /// Returns a Base DN from the LDAP connection string if presented. Otherwise, connects to a LDAP server and consumes Base DN from the RootDSE.
    /// </summary>
    /// <returns>BASE DN.</returns>
    public string GetBaseDn() => _dn.Value;

    private string GetBaseDnInternal(ILdapConnection conn, LdapConnectionString? connectionString = null)
    {
        if (connectionString is null)
        {
            return string.Empty;
        }

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
