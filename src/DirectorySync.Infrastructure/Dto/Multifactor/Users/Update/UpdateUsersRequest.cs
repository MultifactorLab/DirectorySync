using DirectorySync.Application.Models.Core;

namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Update;

internal class UpdateUsersRequest
{
    public ModifiedUserDto[] ModifiedUsers { get; }

    public UpdateUsersRequest(IEnumerable<ModifiedUserDto> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        ModifiedUsers = users.ToArray();
    }
    
    internal static UpdateUsersRequest FromDomainModels(IEnumerable<MemberModel> domainModels)
    {
        ArgumentNullException.ThrowIfNull(domainModels);

        return new UpdateUsersRequest(domainModels.Select(x => new ModifiedUserDto(x.Identity,
            x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value)),
            x.AddedCloudGroups.ToArray(),
            x.RemovedCloudGroups.ToArray())));
    }
}

internal class ModifiedUserDto
{
    public string Identity { get; }

    public UserPropertyDto[] Properties { get; }

    public string[] SignUpGroupsToAdd { get; }
    public string[] SignUpGroupsToRemove { get; }

    public ModifiedUserDto(string identity,
        IEnumerable<UserPropertyDto> properties,
        IEnumerable<string> signUpGroupsToAdd,
        IEnumerable<string> signUpGroupsToRemove)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        Properties = properties.ToArray();
        SignUpGroupsToAdd = signUpGroupsToAdd.ToArray();
        SignUpGroupsToRemove = signUpGroupsToRemove.ToArray();
    }
}
