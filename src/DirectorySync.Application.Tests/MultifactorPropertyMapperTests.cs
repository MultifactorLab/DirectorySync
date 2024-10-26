using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Domain;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Tests;

public class MultifactorPropertyMapperTests
{
    [Fact]
    public void Map_WithoutIdentityAttr_ShouldThrow()
    {
        var mapper = new MultifactorPropertyMapper(Options.Create(new LdapAttributeMappingOptions()));
        Assert.Throws<IdentityAttributeNotDefinedException>(() => mapper.Map([]));
    }
    
    [Fact]
    public void Map_MultipleEmailAttr_ShouldReturnFirstNonNullValue()
    {
        var mapper = new MultifactorPropertyMapper(Options.Create(new LdapAttributeMappingOptions
        {
            IdentityAttribute = "samaccountname",
            EmailAttributes = [ "email", "MAIL" ]
        }));

        LdapAttribute[] attrs = [
            new LdapAttribute(new LdapAttributeName("samaccountname"), "user"),
            new LdapAttribute(new LdapAttributeName("email"), string.Empty),
            new LdapAttribute(new LdapAttributeName("mail"), "mail@mail.com")
            ];

        var dict = mapper.Map(attrs);

        var mail = dict["mail"];
        Assert.Equal("mail@mail.com", mail);
    }
    
    [Fact]
    public void Map_MultiplePhoneAttr_ShouldReturnFirstNonNullValue()
    {
        var mapper = new MultifactorPropertyMapper(Options.Create(new LdapAttributeMappingOptions
        {
            IdentityAttribute = "samaccountname",
            EmailAttributes = ["phone", "mobilephone"]
        }));

        LdapAttribute[] attrs = [
            new LdapAttribute(new LdapAttributeName("samaccountname"), "user"),
            new LdapAttribute(new LdapAttributeName("phone"), null as string),
            new LdapAttribute(new LdapAttributeName("MOBILEPHONE"), "+12345678900")
            ];

        var dict = mapper.Map(attrs);

        var mobilephone = dict["mobilephone"];
        Assert.Equal("+12345678900", mobilephone);
    }
}
