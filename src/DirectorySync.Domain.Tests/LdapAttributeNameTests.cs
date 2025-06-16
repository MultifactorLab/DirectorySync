using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Tests;

public class LdapAttributeNameTests
{
    [Fact]
    public void Equals_CaseInsensitive_ShouldReturnTrue()
    {
        Assert.True(new LdapAttributeName("userprincipalname") == new LdapAttributeName("UserPrincipalName"));
    }
}
