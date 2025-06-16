using System.Security.Cryptography;
using System.Text;
using DirectorySync.Shared.Karnel;

namespace DirectorySync.Application.Models.ValueObjects;

/// <summary>
/// Hash of the LDAP attribute names and its values.
/// </summary>
public class AttributesHash : ValueObject
{
    public string Value { get; }

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

        Value = hash;
    }

    public AttributesHash(LdapAttributeCollection attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var attrs = attributes
            .Select(x =>
            {
                var values = x.Values.Length == 0
                    ? string.Empty
                    : $":{string.Join(',', x.Values)}";
                return $"{x.Name}{values}";
            })
            .OrderByDescending(x => x);

        var joinedAttrs = string.Join(';', attrs);
        var bytes = Encoding.UTF8.GetBytes(joinedAttrs);
        var hash = SHA256.HashData(bytes);

        Value = BitConverter.ToString(hash).Replace("-", string.Empty);
    }
    
    public override string ToString() => $"{nameof(AttributesHash)} '{Value}'";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
