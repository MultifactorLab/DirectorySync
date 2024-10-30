namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Delete;

internal class DeleteUsersDto
{
    public string[] Identities { get; }

    public DeleteUsersDto(IEnumerable<string> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);
        Identities = identities.ToArray();
    }
}
