namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

internal class CreateUsersDto
{
    public NewUserDto[] NewUsers { get; }

    public CreateUsersDto(IEnumerable<NewUserDto> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        NewUsers = users.ToArray();
    }
}

internal class NewUserDto
{
    public string Identity { get; }

    public UserPropertyDto[] Properties { get; }

    public NewUserDto(string identity, IEnumerable<UserPropertyDto> properties)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        Properties = properties.ToArray();
    }
}
