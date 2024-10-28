namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

internal class UpdateUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
