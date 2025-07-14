using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal sealed class LdapDomainDiscovery
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly ILogger<LdapDomainDiscovery> _logger;

    public LdapDomainDiscovery(LdapConnectionFactory connectionFactory,
        ILogger<LdapDomainDiscovery> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public IEnumerable<string> GetForestDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        var childDomains = new List<string>();

        using var connection = _connectionFactory.CreateConnection(options);

        var searchRequest = new SearchRequest(
            $"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}",
            "(objectClass=crossRef)",
            SearchScope.OneLevel,
            "nCName", "dnsRoot", "systemFlags", "netBiosName"
        );

        var response = (SearchResponse)connection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in response.Entries)
        {
            var systemFlags = GetAttributeInt(entry, "systemFlags");
            if ((systemFlags & 0x2) == 0x2)
            {
                var dnsRoot = GetAttribute(entry, "dnsRoot");

                // Исключаем forest root domain
                if (!string.Equals(dnsRoot, schema.NamingContext.StringRepresentation, StringComparison.OrdinalIgnoreCase))
                {
                    childDomains.Add(dnsRoot);
                }
            }
        }

        return childDomains;
    }

    public IEnumerable<string> GetTrustedDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        using var connection = _connectionFactory.CreateConnection(options);

        var trustedDomains = new List<string>();
        var trustedSearchRequest = new SearchRequest(
            $"CN=System,{schema.NamingContext.StringRepresentation}",
            "(objectClass=trustedDomain)",
            SearchScope.Subtree,
            "trustPartner"
        );

        var trustedResponse = (SearchResponse)connection.SendRequest(trustedSearchRequest);

        foreach (SearchResultEntry entry in trustedResponse.Entries)
        {
            var trustPartner = GetAttribute(entry, "trustPartner");

            if (!string.IsNullOrEmpty(trustPartner))
            {
                trustedDomains.Add(trustPartner);
            }
        }

        var forestDomains = GetForestDomainNames(connection, schema);

        var trustedOnly = trustedDomains
            .Where(td => !forestDomains.Contains(td, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return trustedOnly;
    }

    private List<string> GetForestDomainNames(ILdapConnection connection, ILdapSchema schema)
    {
        var forestDomains = new List<string>();

        var searchRequest = new SearchRequest(
            $"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}",
            "(objectClass=crossRef)",
            SearchScope.OneLevel,
            "dnsRoot", "netBiosName", "systemFlags"
        );

        var response = (SearchResponse)connection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in response.Entries)
        {
            var systemFlags = GetAttributeInt(entry, "systemFlags");
            if ((systemFlags & 0x2) == 0x2)
            {
                var dnsRoot = GetAttribute(entry, "dnsRoot");
                var netBiosName = GetAttribute(entry, "netBiosName");

                if (!string.IsNullOrEmpty(dnsRoot))
                {
                    forestDomains.Add(dnsRoot);
                }

                if (!string.IsNullOrEmpty(netBiosName) && !forestDomains.Contains(netBiosName, StringComparer.OrdinalIgnoreCase))
                {
                    forestDomains.Add(netBiosName);
                }
            }
        }

        return forestDomains;
    }

    private string? GetAttribute(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName))
        {
            return entry.Attributes[attributeName][0].ToString();
        }
        return null;
    }

    private int GetAttributeInt(SearchResultEntry entry, string attributeName)
    {
        var val = GetAttribute(entry, attributeName);
        return val != null ? int.Parse(val) : 0;
    }
}
