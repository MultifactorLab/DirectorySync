namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Delete;

internal class DeleteUsersResponse
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
