using DirectorySync.Application.Integrations.Multifactor.Models;

namespace DirectorySync.Application.Integrations.Multifactor;

internal class GroupMappingsOptions
{
    public GroupMapping[] DirectoryGroupMappings { get; set; } = Array.Empty<GroupMapping>();
}
