namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Create;

internal class CreateUsersResponse
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];   
}
