namespace DirectorySync.Domain;

public class MultifactorIdentity : ValueObject
{
    public string Value { get; }
    
    public MultifactorIdentity(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        Value = identity;
    }

    public static implicit operator string(MultifactorIdentity identity)
    {
        if (identity is null)
        {
            throw new InvalidCastException("Identity is null");
        }
        
        return identity.Value;
    }

    public override string ToString() => Value;

    public static MultifactorIdentity FromRawString(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        return new(identity.Trim().ToLower());
    }

    public static MultifactorIdentity FromLdapFormat(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        var lower = identity.Trim().ToLower();

        // netbios
        var index = lower.IndexOf("\\", StringComparison.Ordinal);
        if (index > 0)
        {
            return new(lower[(index + 1)..]);
        }

        // upn
        index = lower.IndexOf("@", StringComparison.Ordinal);
        if (index > 0)
        {
            return new(lower[..index]);
        }

         return new(lower);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
