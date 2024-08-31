namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

public class UserPropertyDto
{
    public string Property { get; }

    public string? Value { get; }

    public UserPropertyDto(string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException($"'{nameof(property)}' cannot be null or whitespace.", nameof(property));
        }

        Property = property;
        Value = value;
    }
}
