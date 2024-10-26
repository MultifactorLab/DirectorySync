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
        Assert.Throws<IdentityAttributeNotDefinedException>(() => mapper.Map(new LdapAttributeCollection([])));
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
        var coll = new LdapAttributeCollection(attrs);

        var dict = mapper.Map(coll);

        var mail = dict[MultifactorPropertyName.EmailProperty];
        Assert.Equal("mail@mail.com", mail);
    }
    
    [Fact]
    public void Map_MultiplePhoneAttr_ShouldReturnFirstNonNullValue()
    {
        var mapper = new MultifactorPropertyMapper(Options.Create(new LdapAttributeMappingOptions
        {
            IdentityAttribute = "samaccountname",
            PhoneAttributes = ["phone", "mobilephone"]
        }));

        LdapAttribute[] attrs = [
            new LdapAttribute(new LdapAttributeName("samaccountname"), "user"),
            new LdapAttribute(new LdapAttributeName("phone"), null as string),
            new LdapAttribute(new LdapAttributeName("MOBILEPHONE"), "+12345678900")
            ];
        var coll = new LdapAttributeCollection(attrs);

        var dict = mapper.Map(coll);

        var mobilephone = dict[MultifactorPropertyName.PhoneProperty];
        Assert.Equal("+12345678900", mobilephone);
    }
}
