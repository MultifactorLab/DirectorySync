namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Create;

internal class CreateUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
