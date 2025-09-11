using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies
{
    internal sealed class ActiveDirectoryDomainDiscovery : IDomainDiscoveryStrategy
    {
        private readonly ILogger _logger;

        public ActiveDirectoryDomainDiscovery(ILogger logger)
        {
            _logger = logger;
        }
        
        public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
        {
            var entries = QueryDomains(connection,
                $"CN=Partitions,CN=Configuration,{schema.NamingContext.StringRepresentation}",
                $"(&({schema.ObjectClass}=crossRef)(systemFlags=3)(nCName=*))",
                SearchScope.Subtree,
                ["dnsRoot"]);
            
            var forestDomains = new List<LdapDomain>();
            
            foreach (SearchResultEntry entry in entries)
            {
                var value = GetAttributeValue(entry, "dnsRoot");

                if (!string.IsNullOrWhiteSpace(value))
                {
                    forestDomains.Add(new LdapDomain(value));
                }
            }
            
            return forestDomains;
        }

        public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
        {
            var entries = QueryDomains(connection,
                $"CN=System,{schema.NamingContext.StringRepresentation}",
                $"(&({schema.ObjectClass}=trustedDomain))",
                SearchScope.Subtree,
                []);
            
            var trustedDomains = new List<LdapDomain>();
            
            foreach (SearchResultEntry entry in entries)
            {
                var value = GetAttributeValue(entry, "trustPartner");

                if (!string.IsNullOrWhiteSpace(value))
                {
                    trustedDomains.Add(new LdapDomain(value));
                }
            }
            
            var forestDomains = FindForestDomains(connection, schema);

            return trustedDomains.Except(forestDomains).ToList();
        }

        private static SearchResultEntryCollection QueryDomains(ILdapConnection connection,
            string dn,
            string filter,
            SearchScope scope,
            string[] attributes)
        {
            var searchRequest = new SearchRequest(dn, filter, scope, attributes);
            var response = (SearchResponse)connection.SendRequest(searchRequest);

            return response.Entries;
        }
        
        private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            if (entry.Attributes.Contains(attributeName))
            {
                DirectoryAttribute attribute = entry.Attributes[attributeName];
                if (attribute.Count > 0)
                {
                    return attribute[0].ToString();
                }
            }
            
            return null;
        }
    }
}
