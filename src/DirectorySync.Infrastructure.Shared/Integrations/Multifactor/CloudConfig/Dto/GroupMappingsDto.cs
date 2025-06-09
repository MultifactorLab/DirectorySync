using System;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto
{
    public class GroupMappingsDto
    {
        public string DirectoryGroup { get; set; } = string.Empty;
        public string[] SignUpGroups { get; set; } = Array.Empty<string>();
    }
}
