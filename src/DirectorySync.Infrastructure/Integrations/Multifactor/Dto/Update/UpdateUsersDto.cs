namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Update;

internal class UpdateUsersDto
{
    public ModifiedUserDto[] ModifiedUsers { get; }

    public UpdateUsersDto(IEnumerable<ModifiedUserDto> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        ModifiedUsers = users.ToArray();
    }
}

internal class ModifiedUserDto
{
    public string Identity { get; }

    public UserPropertyDto[] Properties { get; }

    public SignUpGroupChangesDto SignUpGroupChanges { get; }

    public ModifiedUserDto(string identity,
        IEnumerable<UserPropertyDto> properties,
        SignUpGroupChangesDto signUpGroupChanges)
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
