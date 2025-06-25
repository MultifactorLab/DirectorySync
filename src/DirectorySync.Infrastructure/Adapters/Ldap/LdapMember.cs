using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

internal sealed class LdapMember : ILdapMemberPort
{
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapOptions _ldapOptions;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _ldapAttributeMappingOptions;
    private readonly BaseDnResolver _baseDnResolver;
    private readonly ILogger<LdapMember> _logger;

    public LdapMember(LdapConnectionFactory connectionFactory,
        IOptions<LdapOptions> ldapOptions,
        IOptionsMonitor<LdapAttributeMappingOptions> ldapAttributeMappingOptions,
        BaseDnResolver baseDnResolver,
        ILogger<LdapMember> logger)
    {
        _ldapOptions = ldapOptions.Value;
        _ldapAttributeMappingOptions = ldapAttributeMappingOptions;
        _connectionFactory = connectionFactory;
        _baseDnResolver = baseDnResolver;
        _logger = logger;
    }
    
    public MemberModel? GetByGuid(
    DirectoryGuid objectGuid, 
    string[] requiredAttributes,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objectGuid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var connection = _connectionFactory.CreateConnection();

        var filter = LdapFilters.FindEntryByGuid(objectGuid);
        _logger.LogDebug("Searching member by GUID with filter '{Filter}'", filter);

        var attributesToLoad = requiredAttributes.Concat(["objectGuid"]).Distinct().ToArray();
        var entries = Find(filter, attributesToLoad, connection);
        var entry = entries.FirstOrDefault();

        if (entry == null)
        {
            return null;
        }

        var guid = GetObjectGuid(entry);
        var map = requiredAttributes.Select(entry.GetFirstValueAttribute);
        var attributes = new LdapAttributeCollection(map);
        
        var identity = attributes.GetSingleOrDefault(LdapPropertyOptions.IdentityProperty);
        var properties = GetMemberProperties(attributes, _ldapAttributeMappingOptions.CurrentValue);
        
        return MemberModel.Create(guid, new Identity(identity), properties, new AttributesHash(attributes), []);
    }

    public ReadOnlyCollection<MemberModel> GetByGuids(
        IEnumerable<DirectoryGuid> objectGuids, 
        string[] requiredAttributes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objectGuids);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        var guidList = objectGuids.ToList();
        if (!guidList.Any())
        {
            return new ReadOnlyCollection<MemberModel>(new List<MemberModel>());
        }

        using var connection = _connectionFactory.CreateConnection();

        // В Active Directory нет возможности искать сразу по множеству objectGuid напрямую — 
        // поэтому формируем фильтр с OR-условиями.
        var filter = LdapFilters.FindEntriesByGuids(guidList);
        _logger.LogDebug("Searching members by GUIDs with filter '{Filter}'", filter);

        var attributesToLoad = requiredAttributes.Concat(["objectGuid"]).Distinct().ToArray();
        var entries = Find(filter, attributesToLoad, connection);

        var models = new List<MemberModel>();
        foreach (var entry in entries)
        {
            var guid = GetObjectGuid(entry);
            var map = requiredAttributes.Select(entry.GetFirstValueAttribute);
            var attributes = new LdapAttributeCollection(map);
        
            var identity = attributes.GetSingleOrDefault(_ldapAttributeMappingOptions.CurrentValue.IdentityAttribute);
            var properties = GetMemberProperties(attributes, _ldapAttributeMappingOptions.CurrentValue);
            
            models.Add(MemberModel.Create(guid, new Identity(identity), properties, new AttributesHash(attributes), []));
        }

        return new ReadOnlyCollection<MemberModel>(models);
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

    private IEnumerable<SearchResultEntry> Find(string filter,
        string[] requiredAttributes,
        LdapConnection conn)
    {
        var baseDn = _baseDnResolver.GetBaseDn();
        var searchRequest = new SearchRequest(baseDn,
            filter,
            SearchScope.Subtree,
            requiredAttributes);

        var pageRequestControl = new PageResultRequestControl(_ldapOptions.PageSize);
        var searchOptControl = new SearchOptionsControl(System.DirectoryServices.Protocols.SearchOption.DomainScope);

        searchRequest.Controls.Add(pageRequestControl);
        searchRequest.Controls.Add(searchOptControl);

        var searchResults = new List<SearchResponse>();
        var pages = 0;

        while (true)
        {
            ++pages;

            var response = conn.SendRequest(searchRequest);
            if (response is not SearchResponse searchResponse)
            {
                throw new Exception($"Invalid search response: {response}");
            }

            searchResults.Add(searchResponse);

            var control = searchResponse.Controls
                .OfType<PageResultResponseControl>()
                .FirstOrDefault();
            if (control != null)
            {
                pageRequestControl.Cookie = control.Cookie;
            }

            if (pageRequestControl.Cookie.Length == 0)
            {
                break;
            }
        }

        foreach (var sr in searchResults)
        {
            foreach (SearchResultEntry entry in sr.Entries)
            {
                yield return entry;
            }
        }
    }
    
    private ReadOnlyCollection<MemberProperty> GetMemberProperties(LdapAttributeCollection newAttributes, LdapAttributeMappingOptions options)
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
}
