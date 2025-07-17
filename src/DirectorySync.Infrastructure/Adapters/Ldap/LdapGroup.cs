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

    public GroupModel? GetByGuid(DirectoryGuid objectGuid)
    {
        ArgumentNullException.ThrowIfNull(objectGuid);
        
        _logger.LogDebug("Fetching group by GUID: {Guid}", objectGuid);
        
        var group = FindGroups([objectGuid]).FirstOrDefault();
        
        if (group is not null)
        {
            _logger.LogDebug("Group found for GUID: {Guid}", objectGuid);
        }
        else
        {
            _logger.LogWarning("Group not found for GUID: {Guid}", objectGuid);
        }

        return group;
    }

    public ReadOnlyCollection<GroupModel> GetByGuid(IEnumerable<DirectoryGuid> objectGuids)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);

        var guidList = objectGuids.ToList();
        _logger.LogDebug("Fetching groups for {Count} GUID(s)", guidList.Count);
        
        if (guidList.Count != 0)
        {
            var groups = FindGroups(guidList).AsReadOnly();

            _logger.LogDebug("Fetched {Count} group(s) out of {Requested}", groups.Count, guidList.Count);
            return groups;
        }

        _logger.LogWarning("No GUIDs provided for fetching groups");
        return ReadOnlyCollection<GroupModel>.Empty;
    }

    private List<GroupModel> FindGroups(IEnumerable<DirectoryGuid> objectGuids)
    {
        var foundGroups = new List<GroupModel>();

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        var schema = _ldapSchemaLoader.Load(options);

        var domainsToSearch = new List<string>();
        
        domainsToSearch.AddRange(_ldapDomainDiscovery.GetForestDomains(options, schema));
        
        var trustedDomains = _ldapDomainDiscovery.GetTrustedDomains(options, schema);
        foreach (var trustedDomain in trustedDomains)
        {
            var trustedOptions = GetDomainConnectionOptions(_ldapOptions.Path, trustedDomain, _ldapOptions.Username, _ldapOptions.Password);
            var trustedSchema = _ldapSchemaLoader.Load(trustedOptions);
            domainsToSearch.AddRange(_ldapDomainDiscovery.GetForestDomains(trustedOptions, trustedSchema));
        }

        foreach (var domain in domainsToSearch.Distinct())
        {
            var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, domain, _ldapOptions.Username, _ldapOptions.Password);
            var domainSchema = _ldapSchemaLoader.Load(domainOptions);

            using var connection = _connectionFactory.CreateConnection(domainOptions);

            foreach (var guid in objectGuids)
            {
                if (foundGroups.Any(g => g.Id == guid))
                {
                    continue;
                }

                var group = GetGroup(guid, connection, domainSchema);
                if (group is null)
                {
                    continue;
                }

                foundGroups.Add(group);
                _logger.LogDebug("Group found for GUID {Guid} in {Domain}", guid, domain);
                    
                if (foundGroups.Count == objectGuids.Count())
                {
                    _logger.LogDebug("All requested groups found. Ending search.");
                    return foundGroups;
                }
            }
        }

        return foundGroups;
    }
    
    private GroupModel? GetGroup(DirectoryGuid objectGuid,
        ILdapConnection connection,
        ILdapSchema schema)
    {
        var groupDn = FindGroupDn(objectGuid, connection, schema);
        if (groupDn is null)
        {
            return null;
        }
        
        var members = GetMembersCrossDomain(
            groupDn,
            connection,
            schema
        ).ToList();

        return GroupModel.Create(objectGuid, members);
    }

    private string? FindGroupDn(DirectoryGuid guid, ILdapConnection conn, ILdapSchema schema)
    {
        var filter = LdapFilters.FindGroupByGuid(guid, schema);

        var result = _ldapFinder.Find(filter,
            [schema.Dn],
            schema.NamingContext.StringRepresentation,
            conn);
        
        var first = result.FirstOrDefault();


        return first is null ? null : first.DistinguishedName;
    }

    private IEnumerable<DirectoryGuid> GetMembersCrossDomain(string groupDn,
        ILdapConnection conn,
        ILdapSchema schema)
    {
        var visitedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string GroupDn, ILdapConnection Connection, ILdapSchema Schema)>();
        var pendingCrossForestGroups = new List<string>();
    
        queue.Enqueue((groupDn, conn, schema));
        visitedGroups.Add(groupDn);

        while (queue.Count > 0)
        {
            var (currentGroupDn, currentConn, currentSchema) = queue.Dequeue();

            var filter = $"(&(objectClass={currentSchema.GroupObjectClass.Value})(distinguishedName={currentGroupDn}))";//GetFilter(currentGroupDn, currentSchema);
            var attrs = new[] { "member" };

            var memberResults = _ldapFinder.Find(filter,
                attrs,
                currentSchema.NamingContext.StringRepresentation,
                currentConn);

            foreach (var groupEntry in memberResults)
            {
                var memberDns = groupEntry.Attributes["member"]?.GetValues(typeof(string)).Cast<string>() ?? [];

                foreach (var memberDn in memberDns)
                {
                    if (!visitedGroups.Contains(memberDn))
                    {
                        visitedGroups.Add(memberDn);
                        
                        var targetDomain = LdapDomainExtractor.GetDomainFromDn(memberDn);
                        var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, targetDomain, _ldapOptions.Username, _ldapOptions.Password);
                        var domainSchema = _ldapSchemaLoader.Load(domainOptions);
                        var domainConn = _connectionFactory.CreateConnection(domainOptions);

                        filter = $"(distinguishedName={memberDn})";
                        attrs = new[] { "objectGuid", domainSchema.ObjectClass.Value, "userAccountControl" };
                        
                        var entryResults = _ldapFinder.Find(
                            filter,
                            attrs,
                            domainSchema.NamingContext.StringRepresentation,
                            domainConn);

                        foreach (var entry in entryResults)
                        {
                            var objectClasses = entry.Attributes[domainSchema.ObjectClass]?.GetValues(typeof(string))?.Cast<string>()?.ToList() ?? [];
                            
                            if (objectClasses.Contains(domainSchema.GroupObjectClass.Value, StringComparer.OrdinalIgnoreCase))
                            {
                                if (targetDomain.Equals(currentSchema.Dn, StringComparison.OrdinalIgnoreCase))
                                {
                                    queue.Enqueue((memberDn, domainConn, domainSchema));
                                }
                                else
                                {
                                    pendingCrossForestGroups.Add(memberDn);
                                }
                            }
                            else if (objectClasses.Contains(domainSchema.UserObjectClass.Value, StringComparer.OrdinalIgnoreCase))
                            {
                                var uacValues = entry.Attributes["userAccountControl"]?.GetValues(typeof(string))?.Cast<string>() ?? [];
                                var uac = uacValues.Select(v => int.TryParse(v, out var i) ? i : 0).FirstOrDefault();

                                var disabled = (uac & 0x0002) != 0;
                                if (!disabled)
                                {
                                    yield return GetObjectGuid(entry);
                                }
                            }
                        }
                    }
                }
            }
            
            foreach (var crossForestGroupDn in pendingCrossForestGroups)
            {
                var targetDomain = LdapDomainExtractor.GetDomainFromDn(crossForestGroupDn);
                var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, targetDomain, _ldapOptions.Username, _ldapOptions.Password);
                var domainSchema = _ldapSchemaLoader.Load(domainOptions);
                var domainConn = _connectionFactory.CreateConnection(domainOptions);

                queue.Enqueue((crossForestGroupDn, domainConn, domainSchema));
            }

            pendingCrossForestGroups.Clear();
        }
    }

    private string GetFilter(string groupDn, ILdapSchema schema)
    {
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
