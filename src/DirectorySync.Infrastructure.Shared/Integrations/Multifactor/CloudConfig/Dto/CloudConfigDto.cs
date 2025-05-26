using System;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto
{
    public class CloudConfigDto
    {
        public bool Enabled { get; set; }
        public TimeSpan CloudConfigRefreshTimer { get; set; }
        public TimeSpan SyncTimer { get; set; }
        public TimeSpan ScanTimer { get; set; }

        public TimeSpan TimeoutAD { get; set; }

        public GroupMappingsDto[] DirectoryGroupMappings { get; set; } = Array.Empty<GroupMappingsDto>();
        public bool IncludeNestedGroups { get; set; } = true;

        public PropsMappingDto PropertyMapping { get; set; } = new PropsMappingDto();

    }
}
