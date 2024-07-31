namespace DirectorySync.Domain;

/// <summary>
/// Ldap attribute name.
/// </summary>
public class LdapAttributeName : IComparable<LdapAttributeName>
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

    public int CompareTo(LdapAttributeName? other)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        
        var compareTo = obj as LdapAttributeName;

        if (ReferenceEquals(this, compareTo))
        {
            return true;
        }

        if (ReferenceEquals(null, compareTo))
        {
            return false;
        }

        return Value.Equals(compareTo.Value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(LdapAttributeName a, LdapAttributeName b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        {
            return true;
        }

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(LdapAttributeName a, LdapAttributeName b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
