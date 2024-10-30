using DirectorySync.Application.Exceptions;
using DirectorySync.Domain;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Integrations.Multifactor;

internal class MultifactorPropertyMapper
{
    private readonly LdapAttributeMappingOptions _options;

    public MultifactorPropertyMapper(IOptions<LdapAttributeMappingOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyDictionary<string, string?> Map(LdapAttributeCollection attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        if (string.IsNullOrWhiteSpace(_options.IdentityAttribute))
        {
            throw new IdentityAttributeNotDefinedException();
        }

        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var identity = GetSingle(_options.IdentityAttribute, attributes);
        dict[MultifactorPropertyName.IdentityProperty] = identity;
        
        if (!string.IsNullOrWhiteSpace(_options.NameAttribute)) 
        {
            var name = GetFirstOrNull([_options.NameAttribute], attributes);
            if (name is not null)
            {
                dict[MultifactorPropertyName.NameProperty] = name;
            }
        }
        
        var email = GetFirstOrNull(_options.EmailAttributes, attributes);
        if (email is not null)
        {
            dict[MultifactorPropertyName.EmailProperty] = email;
        }
        
        var phone = GetFirstOrNull(_options.PhoneAttributes, attributes);
        if (phone is not null)
        {
            dict[MultifactorPropertyName.PhoneProperty] = phone;
        }

        return dict;
    }
    
    private static string GetSingle(string name, LdapAttributeCollection attrs)
    {        
        var attr = attrs[name];
        if (attr is null)
        {
            throw new InvalidOperationException($"'{name}' attribute is required");
        }

        var values = attr.GetNotEmptyValues();
        if (values.Length == 0)
        {
            throw new InvalidOperationException($"'{name}' attribute is required");
        }

        if (values.Length != 1)
        {
            throw new InvalidOperationException($"Single '{name}' attribute is required, but more than one was found");
        }

        return values[0];
    }

    private static string? GetFirstOrNull(string[] names, LdapAttributeCollection attrs)
    {
        if (names.Length == 0)
        {
            return default;
        }
        
        foreach (var name in names)
        {
            var attr = attrs[name];
            if (attr is null)
            {
                continue;
            }

            var values = attr.GetNotEmptyValues();
            if (values.Length == 0)
            {
                continue;
            }

            return values[0];
        }

        return default;
    }
}
