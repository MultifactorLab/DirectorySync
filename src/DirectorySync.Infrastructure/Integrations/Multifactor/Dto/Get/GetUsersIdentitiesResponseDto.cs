
using DirectorySync.Application.Integrations.Multifactor.Enums;

namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Get;

internal class GetUsersIdentitiesResponseDto
{
    public string[] Identities { get; init; } = [];
    public UserNameFormat UserNameFormat { get; init; } = UserNameFormat.ActiveDirectory;

}

