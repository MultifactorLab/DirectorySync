using System.Collections;
using System.Text;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain;

// TODO: вынести в app DTO

public class LdapAttributeCollection : IEnumerable<LdapAttribute>
{
    private readonly IReadOnlyDictionary<LdapAttributeName, LdapAttribute> _attributes;

    public LdapAttribute? this[string name]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            var n = new LdapAttributeName(name);
            if (_attributes.TryGetValue(n, out var attr))
            {
                return attr;
            }

            return default;
        }
    }    
    
    public LdapAttribute? this[LdapAttributeName name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            if (_attributes.TryGetValue(name, out var attr))
            {
                return attr;
            }

            return default;
        }
    }

    public LdapAttributeCollection(IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        _attributes = attributes.DistinctBy(x => x.Name).ToDictionary(k => k.Name, v => v);
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"Attributes: {_attributes.Count}");

        if (_attributes.Count == 0)
        {
            return sb.ToString();
        }

        sb.AppendLine();
        foreach (var attribute in _attributes.Take(10))
        {
            sb.AppendLine($"  attribute: {attribute.Value}");
        }

        if (_attributes.Count > 10)
        {
            sb.AppendLine($"  ...and {_attributes.Count - 10} more attributes");
        }

        return sb.ToString();
    }

    public IEnumerator<LdapAttribute> GetEnumerator() => _attributes.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

