namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

internal class DeleteUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
