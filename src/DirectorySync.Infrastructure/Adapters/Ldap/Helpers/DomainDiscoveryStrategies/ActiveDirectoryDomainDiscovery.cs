using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies;

internal sealed class ActiveDirectoryDomainDiscovery : IDomainDiscoveryStrategy
{
    private readonly ILogger _logger;

    public ActiveDirectoryDomainDiscovery(ILogger logger)
    {
        _logger = logger;
    }
    
    public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
    {
        var entries = connection.QueryDomains($"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}",
            $"(&({schema.ObjectClass}=crossRef)(systemFlags=3)(nCName=*))",
            SearchScope.OneLevel,
            ["dnsRoot"]);
        
        var forestDomains = new List<LdapDomain>();
        
        foreach (SearchResultEntry entry in entries)
        {
            var ldapEntry = entry.ToLdapEntry(schema);
            
            var attributes = ldapEntry.Attributes.SafeGetAttributeValues("dnsRoot");

            if (attributes.Length > 0 && !string.IsNullOrWhiteSpace(attributes[0]))
            {
                forestDomains.Add(new LdapDomain(attributes[0]));
            }
        }
        
        return forestDomains;
    }

    public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
    {
        var entries = connection.QueryDomains($"CN=System,{schema.NamingContext.StringRepresentation}",
            $"({schema.ObjectClass}=trustedDomain)",
            SearchScope.OneLevel,
            []);
        
        var trustedDomains = new List<LdapDomain>();
        
        foreach (SearchResultEntry entry in entries)
        {
            var ldapEntry = entry.ToLdapEntry(schema);

            var attributes = ldapEntry.Attributes.SafeGetAttributeValues("trustPartner");
            
            if (attributes.Length > 0 && !string.IsNullOrWhiteSpace(attributes[0]))
            {
                trustedDomains.Add(new LdapDomain(attributes[0]));
            }
        }
        
        var forestDomains = FindForestDomains(connection, schema);

        return trustedDomains.Except(forestDomains).ToList();
    }
}
