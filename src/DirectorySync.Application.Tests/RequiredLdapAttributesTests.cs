using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Workloads;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Tests;

public class RequiredLdapAttributesTests
{
    [Fact]
    public void GetNames_WithoutIdentityAttr_ShouldThrow()
    {
        var attrs = new RequiredLdapAttributes(Options.Create(new LdapAttributeMappingOptions()));

        Assert.Throws<IdentityAttributeNotDefinedException>(() => attrs.GetNames().ToArray());
    }
}
