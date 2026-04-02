using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using FluentAssertions;
using Xunit;

namespace DirectorySync.Infrastructure.Tests.Adapters.Ldap;

public class LdapNamingContextDnsDomainTests
{
    [Theory]
    [InlineData("DC=contoso,DC=com", "contoso.com")]
    [InlineData("DC=child,DC=contoso,DC=com", "child.contoso.com")]
    [InlineData("DC=CHILD,DC=CONTOSO,DC=COM", "CHILD.CONTOSO.COM")]
    public void FromNamingContext_Builds_Dns_Domain(string namingContext, string expectedDns)
    {
        var domain = LdapNamingContextDnsDomain.FromNamingContext(namingContext);
        domain.Value.Should().Be(expectedDns);
    }

    [Fact]
    public void FromNamingContext_Throws_When_No_Dc_Components()
    {
        var act = () => LdapNamingContextDnsDomain.FromNamingContext("CN=foo,OU=bar");
        act.Should().Throw<ArgumentException>();
    }
}
