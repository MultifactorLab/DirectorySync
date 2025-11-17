using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies;

internal sealed class MultiDirectoryDomainDiscovery : IDomainDiscoveryStrategy
{
    private readonly ILogger _logger;

    public MultiDirectoryDomainDiscovery(ILogger logger)
    {
        _logger = logger;
    }
        
    public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
    {
        var entries = connection.QueryDomains($"{schema.NamingContext.StringRepresentation}",
            $"({schema.ObjectClass}=domainDNS)",
            SearchScope.Base,
            []);
            
        var forestDomains = new List<LdapDomain>();
            
        foreach (SearchResultEntry entry in entries)
        {
            var value = entry.GetAttributeValue("name");

            if (!string.IsNullOrWhiteSpace(value))
            {
                forestDomains.Add(new LdapDomain(value));
            }
        }
            
        return forestDomains;
    }

    public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
    {
        return [];
    }
}
