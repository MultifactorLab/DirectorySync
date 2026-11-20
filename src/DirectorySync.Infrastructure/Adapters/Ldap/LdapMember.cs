using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

internal sealed class LdapMember : ILdapMemberPort
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _ldapSchemaLoader;
    private readonly LdapOptions _ldapOptions;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _ldapAttributeMappingOptions;
    private readonly ILogger<LdapMember> _logger;

    public LdapMember(LdapConnectionFactory connectionFactory,
        LdapSchemaLoader ldapSchemaLoader,
        IOptions<LdapOptions> ldapOptions,
        IOptionsMonitor<LdapAttributeMappingOptions> ldapAttributeMappingOptions,
        ILogger<LdapMember> logger)
    {
        _connectionFactory = connectionFactory;
        _ldapSchemaLoader = ldapSchemaLoader;
        _ldapOptions = ldapOptions.Value;
        _ldapAttributeMappingOptions = ldapAttributeMappingOptions;
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

        var mainLdapConnectionString = new LdapConnectionString(_ldapOptions.Path);
        var mainOptions = new LdapConnectionOptions(mainLdapConnectionString,
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);
        var distinctDomains = domainsToSearch.Distinct().ToArray();
        
        ILdapSchema? mainSchema = null;
        LdapDomain? primaryDnsDomain = null;

        if (!_ldapOptions.AllowCrossDomainConnections)
        {
            mainSchema = _ldapSchemaLoader.Load(mainOptions);
            primaryDnsDomain = LdapNamingContextDnsDomain.FromNamingContext(mainSchema.NamingContext.StringRepresentation);
            var filtered = distinctDomains.Where(d => d.Equals(primaryDnsDomain)).ToArray();
            if (filtered.Length == 0)
            {
                _logger.LogWarning(
                    "Cross-domain LDAP connections disabled: requested domain(s) {RequestedDomains} do not match primary directory domain {PrimaryDomain}. Only the primary domain will be queried.",
                    string.Join(", ", distinctDomains.Select(d => d.Value)),
                    primaryDnsDomain.Value);
                distinctDomains = new[] { primaryDnsDomain };
            }
            else
            {
                distinctDomains = filtered;
            }
        }

        var models = new List<MemberModel>();

        foreach (var domain in distinctDomains)
        {
            try
            {
                if (guidList.Count == 0)
                {
                    break;
                }
                
                var isPrimary = primaryDnsDomain is not null && domain.Equals(primaryDnsDomain);
                var domainOptions = isPrimary
                    ? mainOptions
                    : LdapDomainConnectionOptionsFactory.Create(mainLdapConnectionString,
                        domain,
                        _ldapOptions.Username,
                        _ldapOptions.Password,
                        _ldapOptions.Timeout);
                var domainSchema = (isPrimary && mainSchema is not null)
                    ? mainSchema
                    : _ldapSchemaLoader.Load(domainOptions);
                
                using var connection = _connectionFactory.CreateConnection(domainOptions);
                
                var filter = LdapFilters.FindEntriesByGuids(guidList, domainSchema);
                LogFilter(guidList, domain, filter);
                
                var identifierAttr = LdapFilters.GetIdentifierAttribute(domainSchema);
                
                var attributesToLoad = requiredAttributes
                    .Concat([identifierAttr])
                    .Distinct()
                    .ToArray();
 
                var entries = connection.Find(domainSchema.NamingContext,
                    filter,
                    SearchScope.Subtree,
                    _ldapOptions.PageSize,
                    schema: domainSchema,
                    attributes: attributesToLoad.Select(a => new LdapAttributeName(a)).ToArray());
                
                foreach (var entry in entries)
                {
                    var member = MapToMemberModel(entry, requiredAttributes, identifierAttr);
                    models.Add(member);
                    guidList.RemoveAll(g => g.Equals(member.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured while searching in {Domain}.",
                    domain);
            }
        }
        
        _logger.LogDebug("Fetched {Count} member(s) out of {Requested}", 
            models.Count, models.Count + guidList.Count);
        
        return new ReadOnlyCollection<MemberModel>(models);
    }

    private MemberModel MapToMemberModel(LdapEntry entry, string[] requiredAttributes, string identifierAttr)
    {
        var guid = GetObjectGuid(entry, identifierAttr);
        var map = requiredAttributes.Select(ra => entry.Attributes[ra]);

        var attributes = new LdapAttributeCollection(map.Where(la => la is not null).Select(la => la!));

        var identityAttrName = _ldapAttributeMappingOptions.CurrentValue.IdentityAttribute;
        var identity = attributes.GetFirstOrDefault(identityAttrName)
                       ?? throw new InvalidOperationException(
                           $"Required identity attribute '{identityAttrName}' not found on entry. " +
                           $"Available: {string.Join(", ", entry.Attributes.Select(a => a.Name))}");

        var properties = BuildMemberProperties(attributes, _ldapAttributeMappingOptions.CurrentValue);

        return MemberModel.Create(guid, new Identity(identity), properties, new AttributesHash(attributes), []);
    }

    private static DirectoryGuid GetObjectGuid(LdapEntry entry, string identifierAttr)
    {
        var value = entry.Attributes;

        if (!value.Has(new LdapAttributeName(identifierAttr)))
        {
            throw new InvalidLdapAttributeNameException(
                $"Entry does not have identifier attribute '{identifierAttr}'");
        }

        var guidString = value[identifierAttr]?.GetNotEmptyValues().First();

        if (guidString is null)
        {
            throw new ArgumentNullException(identifierAttr,
                $"Couldn't parse identifier from '{identifierAttr}'");
        }

        return new DirectoryGuid(Guid.Parse(guidString));
    }

    private static ReadOnlyCollection<MemberProperty> BuildMemberProperties(LdapAttributeCollection newAttributes, LdapAttributeMappingOptions options)
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

    private void LogFilter(List<DirectoryGuid>? guidList, LdapDomain domain, string filter)
    {
        var previewGuids = guidList?.Take(3).Select(g => g.ToString()).ToArray() ?? [];
        var previewText = string.Join(", ", previewGuids);
        if (guidList?.Count > 3)
        {
            previewText += $", ... +{guidList.Count - 3} more";
        }

        _logger.LogDebug("{Domain}: searching {GuidCount} members by GUIDs: {GuidsPreview}. Filter length: {FilterLength}",
            domain,
            guidList?.Count,
            previewText,
            filter.Length);
    }
}
