using DirectorySync.Application.Models.Core;

namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Create;

internal class CreateUsersRequest
{
    public NewUserDto[] NewUsers { get; }

    public CreateUsersRequest(IEnumerable<NewUserDto> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        NewUsers = users.ToArray();
    }

    internal static CreateUsersRequest FromDomainModels(IEnumerable<MemberModel> domainModels)
    {
        ArgumentNullException.ThrowIfNull(domainModels);

        return new CreateUsersRequest(domainModels.Select(x => new NewUserDto(x.Identity,
            x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value)),
            x.AddedCloudGroups.ToArray())));
    }
}

internal class NewUserDto
{
    public string Identity { get; }
    public UserPropertyDto[] Properties { get; }
    public string[] SignUpGroupsToAdd { get; }

    public NewUserDto(string identity, IEnumerable<UserPropertyDto> properties, string[] signUpGroupsToAdd)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        Properties = properties.ToArray();
        SignUpGroupsToAdd = signUpGroupsToAdd;
    }
}
