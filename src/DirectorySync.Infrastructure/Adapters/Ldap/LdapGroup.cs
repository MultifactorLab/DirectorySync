using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Integrations.Ldap;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;
using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

internal sealed class LdapGroup : ILdapGroupPort
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _ldapSchemaLoader;
    private readonly LdapFinder _ldapFinder;
    private readonly LdapDomainDiscovery _ldapDomainDiscovery;
    private readonly LdapOptions _ldapOptions;
    private readonly LdapRequestOptions _requestOptions;
    private readonly ILogger<LdapGroup> _logger;

    public LdapGroup(LdapConnectionFactory connectionFactory,
        LdapSchemaLoader ldapSchemaLoader,
        LdapFinder ldapFinder,
        LdapDomainDiscovery ldapDomainDiscovery,
        IOptions<LdapOptions> ldapOptions,
        IOptions<LdapRequestOptions> requestOptions,
        ILogger<LdapGroup> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapSchemaLoader = ldapSchemaLoader;
        _ldapFinder = ldapFinder;
        _ldapDomainDiscovery = ldapDomainDiscovery;
        _ldapOptions = ldapOptions.Value;
        _requestOptions = requestOptions.Value;
        _logger = logger;
    }

    public GroupModel? GetByGuidAsync(DirectoryGuid objectGuid)
    {
        ArgumentNullException.ThrowIfNull(objectGuid);

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        var schema = _ldapSchemaLoader.Load(options);

        GroupModel? group;

        var rootForestDomains = _ldapDomainDiscovery.GetForestDomains(options, schema);

        foreach (var rootForestDomain in rootForestDomains)
        {
            var childOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                rootForestDomain,
                _ldapOptions.Username,
                _ldapOptions.Password);

            using var childConnection = _connectionFactory.CreateConnection(childOptions);

            var childSchema = _ldapSchemaLoader.Load(childOptions);

            group = GetGroup(objectGuid, childConnection, childSchema, childOptions);

            if (group is not null)
            {
                return group;
            }
        }

        var trustedDomains = _ldapDomainDiscovery.GetTrustedDomains(options, schema);

        foreach (var trustedDomain in trustedDomains)
        {
            var trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                trustedDomain,
                _ldapOptions.Username,
                _ldapOptions.Password);

            var trustedSchema = _ldapSchemaLoader.Load(trustedOptions);

            var trustedForestDomians = _ldapDomainDiscovery.GetForestDomains(trustedOptions, trustedSchema);

            foreach (var trustedForestDomain in  trustedForestDomians)
            {
                trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                trustedForestDomain,
                _ldapOptions.Username,
                _ldapOptions.Password);

                trustedSchema = _ldapSchemaLoader.Load(trustedOptions);

                using var trustedConnection = _connectionFactory.CreateConnection(trustedOptions);

                group = GetGroup(objectGuid, trustedConnection, trustedSchema, trustedOptions);

                if (group is not null)
                {
                    return group;
                }
            }
        }

        return null;
    }
    
    public ReadOnlyCollection<GroupModel> GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);

        var guidList = objectGuids.ToList();
        if (guidList.Count == 0)
        {
            return ReadOnlyCollection<GroupModel>.Empty;
        }

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        using var connection = _connectionFactory.CreateConnection(options);

        var schema = _ldapSchemaLoader.Load(options);

        var result = new List<GroupModel>();

        foreach (var guid in guidList)
        {
            GroupModel? group = null;

            var rootForestDomains = _ldapDomainDiscovery.GetForestDomains(options, schema);

            foreach (var rootForestDomain in rootForestDomains)
            {
                var childOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                    rootForestDomain,
                    _ldapOptions.Username,
                    _ldapOptions.Password);

                using var childConnection = _connectionFactory.CreateConnection(childOptions);

                var childSchema = _ldapSchemaLoader.Load(childOptions);

                group = GetGroup(guid, childConnection, childSchema, childOptions);

                if (group is not null)
                {
                    result.Add(group);
                    break;
                }
            }

            if (group is not null)
            {
                continue;
            }

            var trustedDomains = _ldapDomainDiscovery.GetTrustedDomains(options, schema);

            foreach (var trustedDomain in trustedDomains)
            {
                var trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                    trustedDomain,
                    _ldapOptions.Username,
                    _ldapOptions.Password);

                var trustedSchema = _ldapSchemaLoader.Load(trustedOptions);

                var trustedForestDomians = _ldapDomainDiscovery.GetForestDomains(trustedOptions, trustedSchema);

                foreach (var trustedForestDomain in trustedForestDomians)
                {
                    trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path,
                    trustedForestDomain,
                    _ldapOptions.Username,
                    _ldapOptions.Password);

                    trustedSchema = _ldapSchemaLoader.Load(trustedOptions);

                    using var trustedConnection = _connectionFactory.CreateConnection(trustedOptions);

                    group = GetGroup(guid, trustedConnection, trustedSchema, trustedOptions);

                    if (group is not null)
                    {
                        result.Add(group);
                        break;
                    }
                }
            }
        }

        return result.AsReadOnly();
    }
    
    private GroupModel? GetGroup(DirectoryGuid objectGuid,
        ILdapConnection connection,
        ILdapSchema schema,
        LdapConnectionOptions options)
    {
        var groupDn = FindGroupDn(objectGuid, connection, schema, options);
        if (groupDn is not null)
        {
            var members = GetMembers(
                groupDn,
                connection,
                schema
            ).ToList();

            return GroupModel.Create(objectGuid, members);
        }
        
        return null;
    }

    private string? FindGroupDn(DirectoryGuid guid, ILdapConnection conn, ILdapSchema schema, LdapConnectionOptions options)
    {
        var filter = LdapFilters.FindGroupByGuid(guid, schema);
        _logger.LogDebug("Searching by group with filter '{Filter:s}'...", filter);

        var result = _ldapFinder.Find(filter,
            [schema.Dn],
            schema.NamingContext.StringRepresentation,
            conn);
        
        var first = result.FirstOrDefault();
        if (first is null)
        {
            return default;
        }
        
        return first.DistinguishedName;
    }

    private IEnumerable<DirectoryGuid> GetMembers(string groupDn,
        ILdapConnection conn,
        ILdapSchema schema)
    {
        var filter = GetFilter(groupDn, schema);
        _logger.LogDebug("Searching by group members with filter '{Filter:s}'...", filter);

        var attrs = new string[] { "ObjectGUID" };

        var result = _ldapFinder.Find(filter,
            attrs,
            schema.NamingContext.StringRepresentation,
            conn);
        
        foreach (var entry in result)
        {
            yield return GetObjectGuid(entry);
        }
    }

    private string GetFilter(string groupDn, ILdapSchema schema)
    {
        if (_requestOptions.IncludeNestedGroups)
        {
            return LdapFilters.FindEnabledGroupMembersByGroupDnRecursively(groupDn);
        }

        return LdapFilters.FindEnabledGroupMembersByGroupDn(groupDn, schema);
    }

    private static DirectoryGuid GetObjectGuid(SearchResultEntry entry)
    {
        var value = entry.Attributes["objectGuid"]?[0];
        if (value is not byte[] bytes)
        {
            throw new InvalidOperationException("Empty GUID");
        }

        return new Guid(bytes);
    }

    private LdapConnectionOptions GetDomainConnectionOptions(string currentConnectionString,
        string domain,
        string username,
        string password)
    {
        var ldapIdentityFormat = NameTypeDetector.GetType(username);

        if (ldapIdentityFormat is null)
        {
            return null;
        }

        var trustUsername = LdapUsernameChanger.ChangeDomain(username, domain, ldapIdentityFormat.Value);
        
        var newUri = LdapUriChanger.ReplaceHostInLdapUrl(currentConnectionString, domain);
        
        return new LdapConnectionOptions(new LdapConnectionString(newUri),
            AuthType.Basic,
            trustUsername,
            password,
            _ldapOptions.Timeout
        );
    }
}
