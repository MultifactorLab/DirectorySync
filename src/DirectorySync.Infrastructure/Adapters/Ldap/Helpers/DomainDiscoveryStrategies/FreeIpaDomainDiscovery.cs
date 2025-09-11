using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies
{
    internal sealed class FreeIpaDomainDiscovery : IDomainDiscoveryStrategy
    {
        private readonly ILogger _logger;

        public FreeIpaDomainDiscovery(ILogger logger)
        {
            _logger = logger;
        }
        
        public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
        {
            _logger.LogDebug("FreeIPA forest discovery: returning base naming context {NamingContext}", schema.NamingContext.StringRepresentation);
            return new List<LdapDomain> { new(schema.NamingContext.StringRepresentation) };
        }

        public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
        {
            var domains = new List<LdapDomain>();

            var searchRequest = new SearchRequest(
                $"cn=trusts,cn=etc,{schema.NamingContext.StringRepresentation}",
                $"({schema.ObjectClass}=ipaNTTrustedDomain)",
                SearchScope.OneLevel,
                "cn"
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            foreach (SearchResultEntry entry in response.Entries)
            {
                if (!entry.Attributes.Contains("cn"))
                {
                    continue;
                }

                var value = entry.Attributes["cn"][0]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    domains.Add(new LdapDomain(value));
                }
            }

            _logger.LogDebug("FreeIPA trusted domains found: {domains}", string.Join(", ", domains));
            return domains;
        }
    }
}
