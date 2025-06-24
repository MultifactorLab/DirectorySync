namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Update;

internal class UpdateUsersResponse
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
