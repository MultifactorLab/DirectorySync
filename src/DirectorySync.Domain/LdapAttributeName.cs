using CSharpFunctionalExtensions;

namespace DirectorySync.Domain;

/// <summary>
/// Ldap attribute name.
/// </summary>
public class LdapAttributeName : ComparableValueObject
{
    public string Value { get; }
    
    public LdapAttributeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Value = name;
    }

    public static implicit operator string(LdapAttributeName name)
    {
        if (name is null)
        {
            throw new InvalidCastException("Name is null");
        }

        return name.Value;
    }
    
    public static implicit operator LdapAttributeName(string name)
    {
        try
        {
            return new(name);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException("Failed to cast", ex);
        }
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}
