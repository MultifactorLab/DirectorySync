using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Domain;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application;

public class MultifactorPropertyMapper
{
    private const string IdentityProperty = "Identity";
    private const string NameProperty = "Name";
    private const string EmailProperty = "Email";
    private const string PhoneProperty = "Phone";
    
    private readonly LdapAttributeMappingOptions _options;

    public MultifactorPropertyMapper(IOptions<LdapAttributeMappingOptions> options)
    {
        _options = options.Value;
    }

    public IEnumerable<KeyValuePair<string, string?>> Map(IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var attrs = attributes.ToArray();
        
        var identity = attrs.FirstOrDefault(x => x.Name == _options.IdentityAttribute);
        if (identity is null || identity.Values.Length == 0)
        {
            throw new InvalidOperationException("Identity property required");
        }

        if (identity.Values.Length != 1)
        {
            throw new InvalidOperationException("Single identity property value is required, but more than one was found");
        }

        yield return new (IdentityProperty, identity.Values[0]);
        
        if (!string.IsNullOrWhiteSpace(_options.NameAttribute))
        {
            var name = attrs.FirstOrDefault(x => x.Name == _options.NameAttribute);
            if (name is not null)
            {
                yield return new (NameProperty, name.Values[0]);
            }
        }
        
        if (_options.EmailAttributes.Length != 0)
        {
            var email = attrs
                .Where(x => _options.EmailAttributes.Contains(x.Name))
                .FirstOrDefault(x => x.Values.Length != 0);
            if (email is not null)
            {
                yield return new (EmailProperty, email.Values[0]);
            }
        }
        
        if (_options.PhoneAttributes.Length != 0)
        {
            var phone = attrs
                .Where(x => _options.PhoneAttributes.Contains(x.Name))
                .FirstOrDefault(x => x.Values.Length != 0);
            if (phone is not null)
            {
                yield return new (PhoneProperty, phone.Values[0]);
            }
        }
    }
}
