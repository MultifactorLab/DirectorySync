namespace DirectorySync.Application.Integrations.Multifactor;

public record MultifactorProperty
{
    public string Name { get; }
    public string? Value { get; }

    public MultifactorProperty(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Value = value;
    }
}
