namespace DirectorySync.Application.Integrations.Multifactor.Models;

internal class GroupMapping
{
    public string DirectoryGroup { get; set; }
    public string[] SignUpGroups { get; set; } = Array.Empty<string>();
}
