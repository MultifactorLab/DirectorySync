using DirectorySync.Domain.Karnel;

namespace DirectorySync.Domain.ValueObjects;

public class Identity : ValueObject
{
    public string Value { get; }
    private string _normalizedValue { get; }
    
    public Identity(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        Value = identity;
        _normalizedValue = Normalize(identity);
    }

    public static implicit operator string(Identity identity)
    {
        if (identity is null)
        {
            throw new InvalidCastException("Identity is null");
        }
        
        return identity.Value;
    }

    public override string ToString() => Value;

    private static string Normalize(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        var lower = identity.Trim().ToLower();

        var index = lower.IndexOf("\\", StringComparison.Ordinal);
        if (index > 0)
        {
            return lower[(index + 1)..];
        }

        index = lower.IndexOf("@", StringComparison.Ordinal);
        if (index > 0)
        {
            return lower[..index];
        }

         return lower;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _normalizedValue;
    }
}
