using Multifactor.Core.Ldap.Attributes;

namespace DirectorySync.Application.Tests.Models.ValueObjects;

public class LdapAttributeNameTests
{
    [Fact]
    public void Equals_CaseInsensitive_ShouldReturnTrue()
    {
        Assert.True(new LdapAttributeName("userprincipalname") == new LdapAttributeName("UserPrincipalName"));
    }
}
