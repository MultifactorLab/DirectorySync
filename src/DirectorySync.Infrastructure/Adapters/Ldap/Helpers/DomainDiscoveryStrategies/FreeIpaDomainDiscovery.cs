using System.DirectoryServices.Protocols;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Abstractions;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Schema;

internal sealed class FreeIpaDomainDiscovery : IDomainDiscoveryStrategy
{
    private readonly ILogger _logger;

    public FreeIpaDomainDiscovery(ILogger logger)
    {
        _logger = logger;
    }

    public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema)
    {
        var fullDn = $"cn=kerberos,{schema.NamingContext.StringRepresentation}";
        var forestDomains = new List<LdapDomain>();

        try
        {
            var entries = connection.QueryDomains(
                fullDn,
                "(objectClass=krbRealmContainer)",
                SearchScope.OneLevel,
                ["cn"]);

            foreach (SearchResultEntry entry in entries)
            {
                var ldapEntry = entry.ToLdapEntry(schema);
                var attributes = ldapEntry.Attributes.SafeGetAttributeValues("cn");
                if (attributes.Length > 0 && !string.IsNullOrWhiteSpace(attributes[0]))
                {
                    forestDomains.Add(new LdapDomain(attributes[0].ToLowerInvariant()));
                }
            }
        }
        catch (DirectoryOperationException ex)
            when (ex.Response?.ResultCode == ResultCode.NoSuchObject)
        {
            _logger.LogDebug(ex, "Kerberos realm container not found at {BaseDn}.", fullDn);
        }
        catch (LdapException ex) when (ex.ErrorCode == 32)
        {
            _logger.LogDebug(ex, "Kerberos realm container not found at {BaseDn}.", fullDn);
        }

        if (forestDomains.Count == 0)
        {
            var primaryDnsDomain = LdapNamingContextDnsDomain
                .FromNamingContext(schema.NamingContext.StringRepresentation);
            _logger.LogDebug(
                "Falling back to primary DNS domain from naming context: {Domain}",
                primaryDnsDomain);
            forestDomains.Add(primaryDnsDomain);
        }

        return forestDomains;
    }

    public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema)
    {
        var fullDn = $"cn=trusts,cn=etc,{schema.NamingContext.StringRepresentation}";

        try
        {
            var entries = connection.QueryDomains(
                fullDn,
                "(objectClass=ipaNTTrustedDomain)",
                SearchScope.OneLevel,
                ["ipaNTTrustPartner"]);

            var trustedDomains = new List<LdapDomain>();
            foreach (SearchResultEntry entry in entries)
            {
                var ldapEntry = entry.ToLdapEntry(schema);
                var attributes = ldapEntry.Attributes.SafeGetAttributeValues("ipaNTTrustPartner");
                if (attributes.Length > 0 && !string.IsNullOrWhiteSpace(attributes[0]))
                {
                    trustedDomains.Add(new LdapDomain(attributes[0]));
                }
            }
            return trustedDomains;
        }
        catch (DirectoryOperationException ex)
            when (ex.Response?.ResultCode == ResultCode.NoSuchObject)
        {
            _logger.LogDebug(ex,
                "FreeIPA trusts container not found at {BaseDn}. No AD trusts configured.",
                fullDn);
            return new List<LdapDomain>();
        }
        catch (LdapException ex) when (ex.ErrorCode == 32)
        {
            _logger.LogDebug(ex,
                "FreeIPA trusts container not found at {BaseDn}. No AD trusts configured.",
                fullDn);
            return new List<LdapDomain>();
        }
    }
}
