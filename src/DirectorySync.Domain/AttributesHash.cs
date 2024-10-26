using CSharpFunctionalExtensions;
using System.Security.Cryptography;
using System.Text;

namespace DirectorySync.Domain;

/// <summary>
/// Hash of the LDAP attribute names and its values.
/// </summary>
public class AttributesHash : ValueObject
{
    private readonly string _value;

    /// <summary>
    /// Creates AttributesHash from the string representation.
    /// </summary>
    /// <param name="hash">Hash string.</param>
    /// <exception cref="ArgumentException"></exception>
    public AttributesHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(hash));
        }

        _value = hash;
    }

    public AttributesHash(IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var attrs = attributes
            .OrderByDescending(x => x.Name)
            .Select(x =>
            {
                var values = x.Values.Length == 0
                    ? string.Empty
                    : $":{string.Join(',', x.Values)}";
                return $"{x.Name}{values}";
            });

        var joinedAttrs = string.Join(';', attrs);
        var bytes = Encoding.UTF8.GetBytes(joinedAttrs);
        var hash = SHA256.HashData(bytes);
        
        _value = BitConverter.ToString(hash).Replace("-", string.Empty);
    }
    
    public static implicit operator string(AttributesHash hash)
    {
        if (hash is null)
        {
            throw new InvalidCastException("Hash is null");
        }

        return hash._value;
    }
    
    public override string ToString() => $"{nameof(AttributesHash)} '{_value}'";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
