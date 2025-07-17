namespace DirectorySync.Application.Models.Core;

public class GroupMapping
{
    public string DirectoryGroup { get; set; } = string.Empty;
    public string[] SignUpGroups { get; set; } = [];
}
