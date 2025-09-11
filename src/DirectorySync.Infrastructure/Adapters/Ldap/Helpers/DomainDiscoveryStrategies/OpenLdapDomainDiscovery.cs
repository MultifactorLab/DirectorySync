using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies
{
    internal sealed class OpenLdapDomainDiscovery : IDomainDiscoveryStrategy
    {
        private readonly ILogger _logger;

        public OpenLdapDomainDiscovery(ILogger logger)
        {
            _logger = logger;
        }
        
        public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
        {
            var domains = new List<LdapDomain> { new (schema.NamingContext.StringRepresentation) };

            var searchRequest = new SearchRequest(
                schema.NamingContext.StringRepresentation,
                $"({schema.ObjectClass}=dcObject)",
                SearchScope.OneLevel,
                "dc"
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                if (!entry.Attributes.Contains("dc"))
                {
                    continue;
                }

                var dc = entry.Attributes["dc"][0]?.ToString();
                if (!string.IsNullOrWhiteSpace(dc))
                {
                    domains.Add(new LdapDomain(dc));
                }
            }

            _logger.LogDebug("OpenLDAP domains discovered: {domains}", string.Join(", ", domains));
            return domains;
        }

        public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
        {
            _logger.LogDebug("OpenLDAP does not support trusted domains.");
            return new List<LdapDomain>();
        }
    }
}
