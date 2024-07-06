namespace DirectorySync.Domain;

public record MultifactorUserId
{
    public string Value { get; }

    public MultifactorUserId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }
        Value = id;
    }

    public static implicit operator string(MultifactorUserId userId)
    {
        if (userId is null)
        {
            throw new InvalidCastException("User id is null");
        }

        return userId.Value;
    }

    public override string ToString() => Value;
}
