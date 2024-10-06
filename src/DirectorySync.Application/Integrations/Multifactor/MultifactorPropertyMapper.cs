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

    public IReadOnlyDictionary<string, string?> Map(LdapAttribute[] attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        if (string.IsNullOrWhiteSpace(_options.IdentityAttribute))
        {
            throw new IdentityAttributeNotDefinedException();
        }

        var dict = new Dictionary<string, string?>();

        var identity = GetSingle(_options.IdentityAttribute, attributes);
        dict[MultifactorPropertyName.IdentityProperty] = identity;
        
        var name = GetFirstOrNull([_options.NameAttribute], attributes);
        if (name is not null)
        {
            dict[MultifactorPropertyName.NameProperty] = name;
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
    
    private static string GetSingle(string name, LdapAttribute[] attrs)
    {
        var n = new LdapAttributeName(name);
        
        var attr = attrs.FirstOrDefault(x => x.Name == n);
        if (attr is null || attr.Values.Length == 0)
        {
            throw new InvalidOperationException($"'{n}' attribute is required");
        }

        if (attr.Values.Length != 1)
        {
            throw new InvalidOperationException($"Single '{n}' attribute is required, but more than one was found");
        }

        return attr.Values[0] ?? throw new InvalidOperationException($"'{n}' attribute value is required");
    }

    private static string? GetFirstOrNull(string?[] names, LdapAttribute[] attrs)
    {
        if (names.Length == 0)
        {
            return default;
        }
        
        var n = names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new LdapAttributeName(x!));

        var attr = attrs
            .Where(x => n.Contains(x.Name))
            .FirstOrDefault(x => x.Values.Length != 0);

        return attr?.Values[0];
    }
}
