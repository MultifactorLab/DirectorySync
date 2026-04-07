using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

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

        return new UpdateUsersRequest(domainModels.Select(x => new ModifiedUserDto(
            x.Identity.Value,
            ExternalDirectoryObjectId.ToCanonicalString(x.Id),
            x.NewIdentity?.Value,
            x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value)),
            x.AddedCloudGroups.ToArray(),
            x.RemovedCloudGroups.ToArray())));
    }
}

internal class ModifiedUserDto
{
    public string Identity { get; }

    public string ExternalObjectId { get; }

    public string? NewIdentity { get; }

    public UserPropertyDto[] Properties { get; }

    public string[] SignUpGroupsToAdd { get; }
    public string[] SignUpGroupsToRemove { get; }

    public ModifiedUserDto(string identity,
        string externalObjectId,
        string? newIdentity,
        IEnumerable<UserPropertyDto> properties,
        IEnumerable<string> signUpGroupsToAdd,
        IEnumerable<string> signUpGroupsToRemove)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalObjectId);
        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        ExternalObjectId = externalObjectId;
        NewIdentity = newIdentity;
        Properties = properties.ToArray();
        SignUpGroupsToAdd = signUpGroupsToAdd.ToArray();
        SignUpGroupsToRemove = signUpGroupsToRemove.ToArray();
    }
}
