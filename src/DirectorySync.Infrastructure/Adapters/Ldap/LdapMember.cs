using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
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
    private readonly LdapSchemaLoader _schemaLoader;
    private readonly LdapOptions _ldapOptions;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _ldapAttributeMappingOptions;
    private readonly BaseDnResolver _baseDnResolver;
    private readonly ILogger<LdapMember> _logger;

    public LdapMember(LdapConnectionFactory connectionFactory,
        LdapSchemaLoader schemaLoader,
        IOptions<LdapOptions> ldapOptions,
        IOptionsMonitor<LdapAttributeMappingOptions> ldapAttributeMappingOptions,
        BaseDnResolver baseDnResolver,
        ILogger<LdapMember> logger)
    {
        _connectionFactory = connectionFactory;
        _schemaLoader = schemaLoader;
        _ldapOptions = ldapOptions.Value;
        _ldapAttributeMappingOptions = ldapAttributeMappingOptions;
        _connectionFactory = connectionFactory;
        _baseDnResolver = baseDnResolver;
        _logger = logger;
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

        var options = new LdapConnectionOptions(new LdapConnectionString(_ldapOptions.Path),
            AuthType.Basic,
            _ldapOptions.Username,
            _ldapOptions.Password,
            _ldapOptions.Timeout);

        using var connection = _connectionFactory.CreateConnection(options);

        var schema = _schemaLoader.Load(options);

        // В Active Directory нет возможности искать сразу по множеству objectGuid напрямую — 
        // поэтому формируем фильтр с OR-условиями.
        var filter = LdapFilters.FindEntriesByGuids(guidList);

        var logText = GetFilterLogText(guidList);
        _logger.LogDebug("Searching {GuidCount} members by GUIDs: {GuidsPreview}. Filter length: {FilterLength}",
            guidList.Count,
            logText,
            filter.Length);

        var attributesToLoad = requiredAttributes.Concat(["objectGuid"]).Distinct().ToArray();
        var entries = Find(filter, attributesToLoad, connection, options);

        var models = new List<MemberModel>();
        foreach (var entry in entries)
        {
            models.Add(GetMember(entry, requiredAttributes));
        }

        return new ReadOnlyCollection<MemberModel>(models);
    }

    private MemberModel GetMember(SearchResultEntry entry, string[] requiredAttributes)
    {
        var guid = GetObjectGuid(entry);
        var map = requiredAttributes.Select(entry.GetFirstValueAttribute);
        var attributes = new LdapAttributeCollection(map);

        var identity = attributes.GetSingleOrDefault(_ldapAttributeMappingOptions.CurrentValue.IdentityAttribute);
        var properties = GetMemberProperties(attributes, _ldapAttributeMappingOptions.CurrentValue);

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

    private IEnumerable<SearchResultEntry> Find(string filter,
        string[] requiredAttributes,
        ILdapConnection conn,
        LdapConnectionOptions options)
    {
        var baseDn = _baseDnResolver.GetBaseDn(options);
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

    private string GetFilterLogText(List<DirectoryGuid?> guidList)
    {
        var previewGuids = guidList.Take(3).Select(g => g.ToString()).ToArray();
        var previewText = string.Join(", ", previewGuids);
        if (guidList.Count() > 3)
        {
            previewText += $", ... +{guidList.Count() - 3} more";
        }

        return previewText;
    }
}
