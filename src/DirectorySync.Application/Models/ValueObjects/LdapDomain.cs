using DirectorySync.Shared.Karnel;

namespace DirectorySync.Application.Models.ValueObjects;

public class LdapDomain : ValueObject
{
    public string Value { get; }
    private readonly string _equalityString;

    public LdapDomain(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        
        Value = value;
        
        _equalityString = Value.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _equalityString;
    }
    
    public override string ToString() => Value;
}
