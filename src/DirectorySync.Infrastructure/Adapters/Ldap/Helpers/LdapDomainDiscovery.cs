using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.DomainDiscoveryStrategies;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal sealed class LdapDomainDiscovery
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly ILogger<LdapDomainDiscovery> _logger;
    
    private readonly ConcurrentDictionary<string, ReadOnlyCollection<LdapDomain>> _forestDomainsCache = new();
    private readonly ConcurrentDictionary<string, ReadOnlyCollection<LdapDomain>> _trustedDomainsCache = new();
    
    public LdapDomainDiscovery(LdapConnectionFactory connectionFactory,
        ILogger<LdapDomainDiscovery> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get the root domain and its child domains (trustType=Parent-Child)
    /// </summary>
    /// <param name="options"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public ReadOnlyCollection<LdapDomain> GetForestDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schema);
        
        var cacheKey = GetCacheKey("forest", options, schema);

        if (_forestDomainsCache.TryGetValue(cacheKey, out var cachedDomains))
        {
            _logger.LogInformation("Forest domains for {Key} founded in cache.", cacheKey);
            return cachedDomains;
        }
        
        _logger.LogDebug("Forest domain detection started for {Host}...", options.ConnectionString.Host);

        using var connection = _connectionFactory.CreateConnection(options);
        
        var strategy = CreateStrategy(schema.LdapServerImplementation, _logger);
        var forestDomains = strategy.FindForestDomains(connection, schema);
        
        var result = forestDomains.AsReadOnly();
        
        _forestDomainsCache[cacheKey] = result;
        
        _logger.LogDebug("Domains found in the forest: {domains}", string.Join(", ", forestDomains));
        return forestDomains.AsReadOnly();
    }

    /// <summary>
    /// Get trusted domains with separate forests (trustType=Forest)
    /// </summary>
    /// <param name="options"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public ReadOnlyCollection<LdapDomain> GetForestTrusts(LdapConnectionOptions options, ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schema);
        
        var cacheKey = GetCacheKey("trusted", options, schema);

        if (_trustedDomainsCache.TryGetValue(cacheKey, out var cachedTrusted))
        {
            _logger.LogInformation("Trusted domains for {Key} founded in cache.", cacheKey);
            return cachedTrusted;
        }
        
        _logger.LogDebug("Trusted external domain detection started...");
        
        using var connection = _connectionFactory.CreateConnection(options);
        
        var strategy = CreateStrategy(schema.LdapServerImplementation, _logger);
        var trustedDomains = strategy.FindForestTrusts(connection, schema);
        
        var result = trustedDomains.ToList().AsReadOnly();

        _trustedDomainsCache[cacheKey] = result;

        _logger.LogDebug("Trusted external domains found: {domains}", string.Join(", ", trustedDomains));

        return result;
    }
    
    private static IDomainDiscoveryStrategy CreateStrategy(LdapImplementation ldapImplementation, ILogger logger) =>
        ldapImplementation switch
        {
            LdapImplementation.ActiveDirectory => new ActiveDirectoryDomainDiscovery(logger),
            LdapImplementation.Samba => new ActiveDirectoryDomainDiscovery(logger),
            LdapImplementation.FreeIPA => new FreeIpaDomainDiscovery(logger),
            LdapImplementation.OpenLDAP => new OpenLdapDomainDiscovery(logger),
            LdapImplementation.MultiDirectory => new MultiDirectoryDomainDiscovery(logger),
            _ => throw new NotSupportedException($"Unknown system type: {nameof(ldapImplementation)}")
        };
    
    private static string GetCacheKey(string type, LdapConnectionOptions options, ILdapSchema schema)
    {
        return $"{type}:{options.ConnectionString.Host}:{schema.NamingContext.StringRepresentation}";
    }
}
