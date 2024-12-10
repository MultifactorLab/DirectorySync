using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Integrations.Multifactor;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal class RequiredLdapAttributes
{
    private readonly object _locker = new();
    private string[] _requiredLdapAttributes = [];

    public RequiredLdapAttributes(IOptionsMonitor<LdapAttributeMappingOptions> options)
    {
        _requiredLdapAttributes = Map(options.CurrentValue).ToArray();
        options.OnChange(x => 
        { 
            lock (_locker)
            {
                _requiredLdapAttributes = Map(x).ToArray();
            }
        });
    }

    public string[] GetNames()
    {
        lock (_locker)
        {
            return _requiredLdapAttributes;
        }
    }

    public IEnumerable<string> Map(LdapAttributeMappingOptions options)
    {
        yield return options.IdentityAttribute;

        if (!string.IsNullOrWhiteSpace(options.NameAttribute))
        {
            yield return options.NameAttribute;
        }

        foreach (var emailAttrName in options.EmailAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return emailAttrName;
        }

        foreach (var phoneAttrName in options.PhoneAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return phoneAttrName;
        }
    }
}
