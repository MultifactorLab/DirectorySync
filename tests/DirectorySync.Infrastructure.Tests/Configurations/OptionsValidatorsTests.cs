using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Adapters.Multifactor;
using DirectorySync.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Tests.Configurations;

public class OptionsValidatorsTests
{
    private readonly MultifactorApiOptionsValidator _multifactorValidator = new();
    private readonly LdapOptionsValidator _ldapValidator = new();
    private readonly StorageOptionsValidator _storageValidator = new();

    [Fact]
    public void MultifactorApiOptionsValidator_AcceptsHttpsAbsoluteUrl()
    {
        var result = _multifactorValidator.Validate(
            Options.DefaultName,
            new MultifactorApiOptions
            {
                Url = "https://api.example.com/",
                Key = "k",
                Secret = "s",
            });

        Assert.False(result.Failed);
    }

    [Fact]
    public void MultifactorApiOptionsValidator_RejectsRelativeUrl()
    {
        var result = _multifactorValidator.Validate(
            Options.DefaultName,
            new MultifactorApiOptions
            {
                Url = "/api",
                Key = "k",
                Secret = "s",
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void LdapOptionsValidator_AcceptsWellFormedLdapUri()
    {
        var result = _ldapValidator.Validate(
            Options.DefaultName,
            new LdapOptions
            {
                Path = "ldap://dc.example.com:389/DC=example,DC=com",
                Username = "u",
                Password = "p",
            });

        Assert.False(result.Failed);
    }

    [Fact]
    public void LdapOptionsValidator_RejectsInvalidPath()
    {
        var result = _ldapValidator.Validate(
            Options.DefaultName,
            new LdapOptions
            {
                Path = "http://example.com:389/DC=example,DC=com",
                Username = "u",
                Password = "p",
            });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("data\\storage.db")]
    [InlineData("data/storage.db")]
    public void StorageOptionsValidator_RejectsPathInFileName(string liteDbFileName)
    {
        var result = _storageValidator.Validate(
            Options.DefaultName,
            new StorageOptions { LiteDbFileName = liteDbFileName });

        Assert.True(result.Failed);
    }

    [Fact]
    public void StorageOptionsValidator_AcceptsSimpleFileName()
    {
        var result = _storageValidator.Validate(
            Options.DefaultName,
            new StorageOptions { LiteDbFileName = "storage.db" });

        Assert.False(result.Failed);
    }
}
