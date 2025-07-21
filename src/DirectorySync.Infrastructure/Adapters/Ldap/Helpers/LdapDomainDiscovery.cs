using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
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

    public ReadOnlyCollection<LdapDomain> GetForestDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schema);
        
        _logger.LogDebug("Forest domain detection started for {Host}...", options.ConnectionString.Host);

        using var connection = _connectionFactory.CreateConnection(options);
        
        var forestDomains = QueryDomains(connection, 
            $"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}", 
            "(objectClass=crossRef)",
            SearchScope.OneLevel, 
            "dnsRoot", entry =>
        {
            var systemFlags = GetAttributeInt(entry, "systemFlags");
            return (systemFlags & 0x2) == 0x2;
        });
        
        forestDomains.RemoveAll(d => d.Equals(new LdapDomain(schema.NamingContext.StringRepresentation)));
        
        _logger.LogDebug("Domains found in the forest: {domains}", string.Join(", ", forestDomains));

        return forestDomains.AsReadOnly();
    }

    public ReadOnlyCollection<LdapDomain> GetTrustedDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schema);
        
        _logger.LogDebug("Trusted external domain detection started...");
        
        using var connection = _connectionFactory.CreateConnection(options);
        
        var trustedDomains = QueryDomains(connection, 
            $"CN=System,{schema.NamingContext.StringRepresentation}", 
            "(objectClass=trustedDomain)",
            SearchScope.Subtree, 
            "trustPartner");

        var forestDomains = QueryDomains(connection, 
            $"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}", 
            "(objectClass=crossRef)",
            SearchScope.OneLevel, 
            "dnsRoot");
        
        var trustedOnly = trustedDomains.Except(forestDomains);

        _logger.LogDebug("Trusted external domains found: {domains}", string.Join(", ", trustedDomains));
        
        return trustedOnly.ToArray().AsReadOnly();
    }

    private List<LdapDomain> QueryDomains(ILdapConnection connection,
        string dn,
        string filter,
        SearchScope scope,
        string attribute,
        Func<SearchResultEntry, bool>? predicate = null)
    {
        var domains = new List<LdapDomain>();

        var searchRequest = new SearchRequest(
            dn,
            filter,
            scope,
            attribute, "systemFlags" // include systemFlags if available for consistency
        );

        var response = (SearchResponse)connection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in response.Entries)
        {
            if (predicate != null && !predicate(entry))
            {
                continue;
            }

            var value = GetAttribute(entry, attribute);
            if (!string.IsNullOrWhiteSpace(value))
            {
                domains.Add(new LdapDomain(value));
            }
        }

        return domains;
    }

    private string? GetAttribute(SearchResultEntry entry, string attributeName)
    {
        return entry.Attributes.Contains(attributeName) ? entry.Attributes[attributeName][0].ToString() : null;
    }

    private int GetAttributeInt(SearchResultEntry entry, string attributeName)
    {
        var val = GetAttribute(entry, attributeName);
        return val != null ? int.Parse(val) : 0;
    }
}
