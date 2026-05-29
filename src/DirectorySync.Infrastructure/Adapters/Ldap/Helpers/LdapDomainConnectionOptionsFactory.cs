using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers.NameResolving;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using System.DirectoryServices.Protocols;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Helpers;

internal static class LdapDomainConnectionOptionsFactory
{
    public static LdapConnectionOptions Create(
        LdapConnectionString mainConnectionString,
        LdapDomain domain,
        string username,
        string password,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(mainConnectionString);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var ldapIdentityFormat = NameTypeDetector.GetType(username)
                                 ?? throw new InvalidOperationException("Unknown username type. Cannot change domain.");
        var trustUsername = LdapUsernameChanger.ChangeDomain(username, domain, ldapIdentityFormat);
        var newLdapConnectionString = LdapUriChanger.ReplaceHostInLdapConnectionString(mainConnectionString, domain);

        return new LdapConnectionOptions(newLdapConnectionString,
            AuthType.Basic,
            trustUsername,
            password,
            timeout);
    }
}
