using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Integrations.Multifactor;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal class RequiredLdapAttributes
{
    private readonly LdapAttributeMappingOptions _options;

    public RequiredLdapAttributes(IOptions<LdapAttributeMappingOptions> options)
    {
        _options = options.Value;
    }

    public IEnumerable<string> GetNames()
    {
        if (string.IsNullOrWhiteSpace(_options.IdentityAttribute))
        {
            throw new IdentityAttributeNotDefinedException();
        }

        yield return _options.IdentityAttribute;

        if (!string.IsNullOrWhiteSpace(_options.NameAttribute))
        {
            yield return _options.NameAttribute;
        }

        foreach (var emailAttrName in _options.EmailAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return emailAttrName;
        }

        foreach (var phoneAttrName in _options.PhoneAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return phoneAttrName;
        }
    }
}
