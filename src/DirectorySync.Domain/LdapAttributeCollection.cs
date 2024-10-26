namespace DirectorySync.Domain;

public class LdapAttributeCollection
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

    public LdapAttributeCollection(IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        _attributes = attributes.ToDictionary(k => k.Name, v => v);
    }
}

