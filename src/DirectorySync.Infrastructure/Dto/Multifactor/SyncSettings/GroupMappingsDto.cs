using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;

namespace DirectorySync.Infrastructure.Dto.Multifactor.SyncSettings;

public class GroupMappingsDto
{
    public string DirectoryGroup { get; set; } = string.Empty;
    public string[] SignUpGroups { get; set; } = Array.Empty<string>();

    public static GroupMapping ToModel(GroupMappingsDto dto)
    {
        return new GroupMapping { DirectoryGroup = dto.DirectoryGroup, SignUpGroups = dto.SignUpGroups, };
    }
}
