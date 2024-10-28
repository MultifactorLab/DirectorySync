namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

internal class CreateUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
