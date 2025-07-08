using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Tests.Models.ValueObjects;

public class AttributesHashTests
{
    [Fact]
    public void Equals_DifferentOrder_ShouldReturnTrue()
    {
        var leftAttrs = new LdapAttributeCollection(new[]
        {
            new LdapAttribute("attr1", "value1"),
            new LdapAttribute("attr2", "value2")
        });

        
        var rightAttrs = new LdapAttributeCollection(new[]
        {
            new LdapAttribute("attr2", "value2"),
            new LdapAttribute("attr1", "value1")
        });
        
        Assert.True(new AttributesHash(leftAttrs) == new AttributesHash(rightAttrs));
    }
    
    [Fact]
    public void WithNullValue()
    {
        var left = new LdapAttributeCollection(new[]
        {
            new LdapAttribute("attr1", null as string)
        });
        
        var right = new LdapAttributeCollection(new[]
        {
            new LdapAttribute("attr1", null as string)
        });
        
        Assert.True(new AttributesHash(left) == new AttributesHash(right));
    }
}
