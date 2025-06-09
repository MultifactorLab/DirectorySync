namespace DirectorySync.Infrastructure.Integrations.Multifactor.Dto;

internal class SignUpGroupChangesDto
{
    public string[] SignUpGroupsToAdd { get; }
    public string[] SignUpGroupsToRemove { get; }

    public SignUpGroupChangesDto(string[] groupsToAdd, string[] groupsToRemove)
    {
        SignUpGroupsToAdd = groupsToAdd;
        SignUpGroupsToRemove = groupsToRemove;
    }
}
