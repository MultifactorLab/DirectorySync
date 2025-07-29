using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Integrations.Ldap;
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
    private readonly ILogger<LdapGroup> _logger;

    public LdapGroup(LdapConnectionFactory connectionFactory,
        LdapSchemaLoader ldapSchemaLoader,
        LdapFinder ldapFinder,
        LdapDomainDiscovery ldapDomainDiscovery,
        IOptions<LdapOptions> ldapOptions,
        ILogger<LdapGroup> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapSchemaLoader = ldapSchemaLoader;
        _ldapFinder = ldapFinder;
        _ldapDomainDiscovery = ldapDomainDiscovery;
        _ldapOptions = ldapOptions.Value;
        _logger = logger;
    }

    public (GroupModel? groups, ReadOnlyCollection<LdapDomain> domains) GetByGuid(DirectoryGuid objectGuid)
    {
        ArgumentNullException.ThrowIfNull(objectGuid);
        
        _logger.LogDebug("Fetching group by GUID: {Guid}", objectGuid);
        
        var (groups, domains) = FindGroups([objectGuid]);

        _logger.LogDebug(groups is not null
            ? "Group found for GUID: {Guid}"
            : "Group not found for GUID: {Guid}", objectGuid);

        return (groups.FirstOrDefault(), domains.AsReadOnly());
    }

    public (ReadOnlyCollection<GroupModel> groups, ReadOnlyCollection<LdapDomain> domains) GetByGuid(IEnumerable<DirectoryGuid> objectGuids)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);

        var guidList = objectGuids.ToList();
        _logger.LogDebug("Fetching groups for {Count} GUID(s)", guidList.Count);

        if (guidList.Count == 0)
        {
            _logger.LogWarning("No GUIDs provided for fetching groups");
            return (ReadOnlyCollection<GroupModel>.Empty, ReadOnlyCollection<LdapDomain>.Empty);
        }

        var (groups, domains) = FindGroups(guidList);
        _logger.LogDebug("Fetched {Count} group(s) out of {Requested}", groups.Count, guidList.Count);
        return (groups.AsReadOnly(), domains.AsReadOnly());
    }

    private (List<GroupModel> groups, List<LdapDomain> domains)FindGroups(IEnumerable<DirectoryGuid> objectGuids)
    {
        var foundGroups = new List<GroupModel>();
        var searchDomains = new HashSet<LdapDomain>();

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        var schema = _ldapSchemaLoader.Load(options);

        var domainsToSearch = GetAllDomains(options, schema).Distinct().ToArray();

        if (domainsToSearch.Length == 0)
        {
            throw new ApplicationException("No Domains provided for fetching groups");
        }

        var guidSet = objectGuids.ToHashSet();

        foreach (var domain in domainsToSearch)
        {
            var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, domain, _ldapOptions.Username, _ldapOptions.Password);
            var domainSchema = _ldapSchemaLoader.Load(domainOptions);

            using var connection = _connectionFactory.CreateConnection(domainOptions);

            try
            {
                foreach (var guid in guidSet)
                {
                    var group = GetGroup(guid, domain, connection, domainSchema, searchDomains);
                    if (group is null)
                    {
                        continue;
                    }
                    
                    foundGroups.Add(group);
                    _logger.LogDebug("Group found for GUID {Guid} in {Domain}", guid, domain);

                    if (foundGroups.Count != guidSet.Count)
                    {
                        continue;
                    }

                    _logger.LogDebug("All requested groups found. Ending search.");
                    return (foundGroups, searchDomains.ToList());
                }
            }
            catch (LdapException ex)
            {
                _logger.LogDebug("Ldap exception occured while searching in {Domain}. Status: {Status}.",
                    domain,
                    ex.ErrorCode);
            }
        }

        return (foundGroups, searchDomains.ToList());
    }
    
    private GroupModel? GetGroup(DirectoryGuid objectGuid,
        LdapDomain domain,
        ILdapConnection connection,
        ILdapSchema schema,
        HashSet<LdapDomain> domainsToSearch)
    {
        var groupDn = FindGroupDn(objectGuid, connection, schema);
        if (groupDn is null)
        {
            return null;
        }
        
        var members = GetMembersCrossDomain(groupDn,
            domain,
            connection,
            schema,
            domainsToSearch).ToList();

        return GroupModel.Create(objectGuid, members);
    }

    private string? FindGroupDn(DirectoryGuid guid, ILdapConnection conn, ILdapSchema schema)
    {
        var filter = LdapFilters.FindGroupByGuid(guid, schema);

        var result = _ldapFinder.Find(filter,
            [schema.Dn],
            schema.NamingContext.StringRepresentation,
            conn);
        
        return result.FirstOrDefault()?.DistinguishedName;
    }

    private IEnumerable<DirectoryGuid> GetMembersCrossDomain(string groupDn,
        LdapDomain initialDomain,
        ILdapConnection initialConn,
        ILdapSchema initialSchema,
        HashSet<LdapDomain> domainsToSearch)
    {
        var visitedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { groupDn };
        var queue = new Queue<(string GroupDn, LdapDomain Domain, ILdapConnection Conn, ILdapSchema Schema)>();
        queue.Enqueue((groupDn, initialDomain, initialConn, initialSchema));

        while (queue.Count > 0)
        {
            var (currentGroupDn, currentDomain, conn, schema) = queue.Dequeue();

            var members = GetGroupMembers(currentGroupDn, conn, schema).ToArray();

            if (members.Length > 0)
            {
                domainsToSearch.Add(currentDomain);
            }

            foreach (var userGuid in members)
            {
                yield return userGuid;
            }

            foreach (var nestedGroupDn in GetNestedGroups(currentGroupDn, conn, schema))
            {
                if (!visitedGroups.Add(nestedGroupDn))
                {
                    continue;
                }

                var targetDomain = LdapDomainExtractor.GetDomainFromDn(nestedGroupDn);
                var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, targetDomain, _ldapOptions.Username, _ldapOptions.Password);
                var domainSchema = _ldapSchemaLoader.Load(domainOptions);
                var domainConn = _connectionFactory.CreateConnection(domainOptions);

                queue.Enqueue((nestedGroupDn, targetDomain, domainConn, domainSchema));
            }
        }
    }

    private IEnumerable<DirectoryGuid> GetGroupMembers(string groupDn, ILdapConnection conn, ILdapSchema schema)
    {
        var filter = LdapFilters.FindEnabledGroupMembersByGroupDn(groupDn, schema);
        var results = _ldapFinder.Find(filter, ["objectGuid"], schema.NamingContext.StringRepresentation, conn);

        foreach (var entry in results)
        {
            yield return GetObjectGuid(entry);
        }
    }

    private IEnumerable<string> GetNestedGroups(string groupDn, ILdapConnection conn, ILdapSchema schema)
    {
        var filter = LdapFilters.FindGroupByDn(groupDn, schema);
        var results = _ldapFinder.Find(filter, ["member"], schema.NamingContext.StringRepresentation, conn);

        var memberDns = results
            .SelectMany(e => e.Attributes["member"]?.GetValues(typeof(string)).Cast<string>() ?? Enumerable.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var userDns = results.SelectMany(e =>
            _ldapFinder.Find(LdapFilters.FindEnabledGroupMembersByGroupDn(groupDn, schema), ["distinguishedName"], schema.NamingContext.StringRepresentation, conn)
            .Select(u => u.DistinguishedName)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        memberDns.ExceptWith(userDns);
        return memberDns;
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

    private IEnumerable<LdapDomain> GetAllDomains(LdapConnectionOptions options, ILdapSchema schema)
    {
        var domains = _ldapDomainDiscovery.GetForestDomains(options, schema).ToList();
        foreach (var trustedDomain in _ldapDomainDiscovery.GetTrustedDomains(options, schema))
        {
            try
            {
                var trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path, trustedDomain, _ldapOptions.Username, _ldapOptions.Password);
                var trustedSchema = _ldapSchemaLoader.Load(trustedOptions);
                domains.AddRange(_ldapDomainDiscovery.GetForestDomains(trustedOptions, trustedSchema));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to get trusted domain {trustedDomain}");
            }
        }

        return domains;
    }

    private LdapConnectionOptions GetDomainConnectionOptions(string currentConnectionString,
        LdapDomain domain,
        string username,
        string password)
    {
        var ldapIdentityFormat = NameTypeDetector.GetType(username);

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
