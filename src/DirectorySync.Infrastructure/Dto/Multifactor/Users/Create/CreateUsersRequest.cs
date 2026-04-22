using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

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

        return new CreateUsersRequest(domainModels.Select(x => new NewUserDto(
            x.Identity.Value,
            ExternalDirectoryObjectId.ToCanonicalString(x.Id),
            x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value)),
            x.AddedCloudGroups.ToArray())));
    }
}

internal class NewUserDto
{
    public string Identity { get; }
    
    public string ExternalObjectId { get; }

    public UserPropertyDto[] Properties { get; }
    public string[] SignUpGroupsToAdd { get; }

    public NewUserDto(string identity, string externalObjectId, IEnumerable<UserPropertyDto> properties, string[] signUpGroupsToAdd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalObjectId);
        ArgumentNullException.ThrowIfNull(properties);

        Identity = identity;
        ExternalObjectId = externalObjectId;
        Properties = properties.ToArray();
        SignUpGroupsToAdd = signUpGroupsToAdd;
    }
}
