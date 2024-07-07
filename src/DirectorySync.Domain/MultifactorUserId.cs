namespace DirectorySync.Domain;

public record MultifactorUserId
{
    public string Value { get; }

    public static MultifactorUserId Undefined => new MultifactorUserId();

    public MultifactorUserId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }
        Value = id;
    }

    private MultifactorUserId()
    {
        Value = "__UNDEFINED__";
    }

    public static implicit operator string(MultifactorUserId userId)
    {
        if (userId is null)
        {
            throw new InvalidCastException("User id is null");
        }

        if (userId == Undefined)
        {
            throw new InvalidCastException("User id is undefined");
        }
        
        return userId.Value;
    }

    public override string ToString() => Value;
}
