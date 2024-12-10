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

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
