
namespace DirectorySync.Application.Integrations.Multifactor.Models;

public class SignUpGroupChanges
{
    public string[] SignUpGroupsToAdd { get; set; } = Array.Empty<string>();
    public string[] SignUpGroupsToRemove { get; set; } = Array.Empty<string>();
}

