namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Update;

internal class UpdateUsersResponseDto
{
    public UserProcessingFailureDto[] Failures { get; init; } = [];
}
