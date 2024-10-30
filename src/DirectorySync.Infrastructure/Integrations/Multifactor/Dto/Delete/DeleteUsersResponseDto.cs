namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Delete;

internal class DeleteUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
