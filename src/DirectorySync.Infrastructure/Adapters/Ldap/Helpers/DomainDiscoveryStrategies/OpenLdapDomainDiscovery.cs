using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies;

internal sealed class OpenLdapDomainDiscovery : IDomainDiscoveryStrategy
{
    private readonly ILogger _logger;

    public OpenLdapDomainDiscovery(ILogger logger)
    {
        _logger = logger;
    }
        
    public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
    {
        // Преобразуем DC-компоненты в DNS-домен
        var dn = schema.NamingContext.StringRepresentation;
        var dnsDomain = string.Join(".",
            dn.Split(',')
                .Where(p => p.StartsWith("dc=", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Substring(3)));

        return [new LdapDomain(dnsDomain)];
    }

    public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
    {
        return [];
    }
}
