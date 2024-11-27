﻿using System.DirectoryServices.Protocols;
using CSharpFunctionalExtensions;
using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchOption = System.DirectoryServices.Protocols.SearchOption;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

internal class GetReferenceGroupWithLdapConnection : IGetReferenceGroup
{
    private readonly LdapOptions _ldapOptions;
    private readonly LdapConnectionFactory _connectionFactory;
    private readonly BaseDnResolver _baseDnResolver;
    private readonly ILogger<GetReferenceGroupWithLdapConnection> _logger;

    public GetReferenceGroupWithLdapConnection(LdapConnectionFactory connectionFactory,
        IOptions<LdapOptions> ldapOptions,
        BaseDnResolver baseDnResolver,
        ILogger<GetReferenceGroupWithLdapConnection> logger)
    {
        _ldapOptions = ldapOptions.Value;
        _connectionFactory = connectionFactory;
        _baseDnResolver = baseDnResolver;
        _logger = logger;
    }

    public ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(requiredAttributes);

        using var connection = _connectionFactory.CreateConnection();

        var groupDn = FindGroupDn(guid, connection);
        if (groupDn is null)
        {
            throw new GroupNotFoundException($"Group with GUID '{guid}' was not found");
        }

        var members = GetMembers(groupDn, requiredAttributes, connection);
        return new ReferenceDirectoryGroup(guid, members);
    }

    private string? FindGroupDn(DirectoryGuid guid, LdapConnection conn)
    {
        var filter = $"(&(objectCategory=group)(objectGUID={guid.OctetString}))";
        _logger.LogDebug("Searching by group with filter '{Filter:s}'...", filter);

        var result = Find(filter, ["distinguishedName"], conn);
        var first = result.FirstOrDefault();
        if (first is null)
        {
            return default;
        }

        return first.DistinguishedName;
    }

    private IEnumerable<ReferenceDirectoryUser> GetMembers(string groupDn, 
        string[] requiredAttributes, 
        LdapConnection conn)
    {
        var filter = $"(&(objectClass=user)(memberof={groupDn}))";
        _logger.LogDebug("Searching by group members with filter '{Filter:s}'...", filter);
        var attrs = requiredAttributes.Concat(["ObjectGUID"]).ToArray();

        var result = Find(filter, attrs, conn);
        foreach (var entry in result)
        {
            var guid = GetObjectGuid(entry);
            var map = requiredAttributes.Select(x => entry.GetSingleValuedAttribute(x));
            var attributes = new LdapAttributeCollection(map);
            yield return new ReferenceDirectoryUser(guid, attributes);
        }
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
        var searchOptControl = new SearchOptionsControl(SearchOption.DomainScope);

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
}