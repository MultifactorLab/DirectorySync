using DirectorySync.Application.Integrations.Multifactor;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Tests;

public class RequiredLdapAttributesTests
{
    [Fact]
    public void Test1()
    {
        var attrs = new RequiredLdapAttributes(Options.Create<LdapAttributeMappingOptions>(new LdapAttributeMappingOptions()));
    }
}
