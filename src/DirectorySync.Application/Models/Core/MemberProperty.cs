namespace DirectorySync.Application.Models.Core;

public record MemberProperty
{
    public string Name { get; }
    public string? Value { get; }

    public MemberProperty(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Value = value;
    }
}
