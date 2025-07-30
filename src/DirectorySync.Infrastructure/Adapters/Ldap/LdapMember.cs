using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
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

namespace DirectorySync.Infrastructure.Adapters.Ldap;

internal sealed class LdapMember : ILdapMemberPort
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _ldapSchemaLoader;
    private readonly LdapFinder _ldapFinder;
    private readonly LdapOptions _ldapOptions;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _ldapAttributeMappingOptions;
    private readonly ILogger<LdapMember> _logger;

    public LdapMember(LdapConnectionFactory connectionFactory,
        LdapSchemaLoader ldapSchemaLoader,
        LdapFinder ldapFinder,
        IOptions<LdapOptions> ldapOptions,
        IOptionsMonitor<LdapAttributeMappingOptions> ldapAttributeMappingOptions,
        ILogger<LdapMember> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapSchemaLoader = ldapSchemaLoader;
        _ldapFinder = ldapFinder;
        _ldapOptions = ldapOptions.Value;
        _ldapAttributeMappingOptions = ldapAttributeMappingOptions;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public ReadOnlyCollection<MemberModel> GetByGuids(
        IEnumerable<DirectoryGuid> objectGuids,
        string[] requiredAttributes,
        LdapDomain[] domainsToSearch)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        var guidList = objectGuids.ToList();
        if (guidList.Count == 0)
        {
            return new ReadOnlyCollection<MemberModel>(Array.Empty<MemberModel>());
        }
        
        _logger.LogDebug("Fetching members for {Count} GUID(s)", guidList.Count);
        
        var models = new List<MemberModel>();

        foreach (var domain in domainsToSearch.Distinct())
        {
            if (guidList.Count == 0)
            {
                break;
            }
            
            var domainOptions = GetDomainConnectionOptions(_ldapOptions.Path, domain, _ldapOptions.Username, _ldapOptions.Password);
            var domainSchema = _ldapSchemaLoader.Load(domainOptions);
            
            using var connection = _connectionFactory.CreateConnection(domainOptions);
            
            // В Active Directory нет возможности искать сразу по множеству objectGuid напрямую — 
            // поэтому формируем фильтр с OR-условиями.
            var filter = LdapFilters.FindEntriesByGuids(guidList);

            LogFilter(guidList, domain, filter);

            var attributesToLoad = requiredAttributes.Concat(["objectGuid"]).Distinct().ToArray();
            var entries = _ldapFinder.Find(filter,
                attributesToLoad,
                domainSchema.NamingContext.StringRepresentation,
                connection);
            
            foreach (var entry in entries)
            {
                var member = MapToMemberModel(entry, requiredAttributes);
                models.Add(member);
                guidList.RemoveAll(g => g.Equals(member.Id));
            }
        }
        
        _logger.LogDebug("Fetched {Count} member(s) out of {Requested}", models.Count, guidList.Count);
        
        return new ReadOnlyCollection<MemberModel>(models);
    }

    private MemberModel MapToMemberModel(SearchResultEntry entry, string[] requiredAttributes)
    {
        var guid = GetObjectGuid(entry);
        var map = requiredAttributes.Select(entry.GetFirstValueAttribute);
        var attributes = new LdapAttributeCollection(map);

        var identity = attributes.GetSingleOrDefault(_ldapAttributeMappingOptions.CurrentValue.IdentityAttribute);
        var properties = BuildMemberProperties(attributes, _ldapAttributeMappingOptions.CurrentValue);

        return MemberModel.Create(guid, new Identity(identity), properties, new AttributesHash(attributes), []);
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

    private ReadOnlyCollection<MemberProperty> BuildMemberProperties(LdapAttributeCollection newAttributes, LdapAttributeMappingOptions options)
    {
        var properties = new List<MemberProperty>();
        var identity = newAttributes.GetSingleOrDefault(options.IdentityAttribute);
        if (identity is not null)
        {
            properties.Add(new MemberProperty(LdapPropertyOptions.IdentityProperty, identity));
        }

        if (!string.IsNullOrWhiteSpace(options.NameAttribute))
        {
            var name = newAttributes.GetFirstOrDefault(options.NameAttribute);
            if (name is not null)
            {
                properties.Add(new MemberProperty(LdapPropertyOptions.AdditionalProperties.NameProperty, name));
            }
        }

        var email = newAttributes.GetFirstOrDefault(options.EmailAttributes);
        if (email is not null)
        {
            properties.Add(new MemberProperty(LdapPropertyOptions.AdditionalProperties.EmailProperty, email));
        }

        var phone = newAttributes.GetFirstOrDefault(options.PhoneAttributes);
        if (phone is not null)
        {
            properties.Add(new MemberProperty(LdapPropertyOptions.AdditionalProperties.PhoneProperty, phone));
        }

        return properties.AsReadOnly();
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
    
    private void LogFilter(List<DirectoryGuid?> guidList, LdapDomain domain, string filter)
    {
        var previewGuids = guidList.Take(3).Select(g => g.ToString()).ToArray();
        var previewText = string.Join(", ", previewGuids);
        if (guidList.Count > 3)
        {
            previewText += $", ... +{guidList.Count - 3} more";
        }

        _logger.LogDebug("{Domain}: searching {GuidCount} members by GUIDs: {GuidsPreview}. Filter length: {FilterLength}",
            domain,
            guidList.Count,
            previewText,
            filter.Length);
    }
}
