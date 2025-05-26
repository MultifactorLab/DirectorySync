namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Create;

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
    public SignUpGroupChangesDto SignUpGroupChanges { get; }

    public NewUserDto(string identity, IEnumerable<UserPropertyDto> properties, SignUpGroupChangesDto signUpGroupChanges)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        Properties = properties.ToArray();
        SignUpGroupChanges = signUpGroupChanges;
    }
}
