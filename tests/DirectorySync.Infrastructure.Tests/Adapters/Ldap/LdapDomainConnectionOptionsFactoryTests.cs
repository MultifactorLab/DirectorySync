using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using FluentAssertions;
using Multifactor.Core.Ldap;

namespace DirectorySync.Infrastructure.Tests.Adapters.Ldap;

public class LdapDomainConnectionOptionsFactoryTests
{
    [Fact]
    public void Create_Uses_NonTls_Connection_For_Trusted_Domain_When_Main_Is_Ldap_389()
    {
        var mainConnectionString = new LdapConnectionString("LDAP://main.contoso.local:389/DC=main,DC=contoso,DC=local");
        var trustedDomain = new LdapDomain("trusted.contoso.local");

        var options = LdapDomainConnectionOptionsFactory.Create(mainConnectionString,
            trustedDomain,
            "svc@main.contoso.local",
            "password",
            TimeSpan.FromSeconds(30));

        options.ConnectionString.Host.Should().Be("trusted.contoso.local");
        options.ConnectionString.Port.Should().Be(389);
        options.ConnectionString.Container.Should().Be("DC=main,DC=contoso,DC=local");
    }

    [Fact]
    public void Create_Uses_Tls_Connection_For_Trusted_Domain_When_Main_Is_Ldaps()
    {
        var mainConnectionString = new LdapConnectionString("LDAPS://main.contoso.local/DC=main,DC=contoso,DC=local");
        var trustedDomain = new LdapDomain("trusted.contoso.local");

        var options = LdapDomainConnectionOptionsFactory.Create(mainConnectionString,
            trustedDomain,
            "svc@main.contoso.local",
            "password",
            TimeSpan.FromSeconds(30));

        options.ConnectionString.Host.Should().Be("trusted.contoso.local");
        options.ConnectionString.Port.Should().Be(636);
        options.ConnectionString.Container.Should().Be("DC=main,DC=contoso,DC=local");
    }
}
