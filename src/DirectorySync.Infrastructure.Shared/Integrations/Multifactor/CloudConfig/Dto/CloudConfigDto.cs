using System;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto
{
    public class CloudConfigDto
    {
        public bool Enabled { get; set; }
        public TimeSpan CloudConfigRefreshTimer { get; set; }
        public TimeSpan SyncTimer { get; set; }
        public TimeSpan ScanTimer { get; set; }

        public string[] DirectoryGroups { get; set; } = Array.Empty<string>();

        public PropsMappingDto PropertyMapping { get; set; } = new PropsMappingDto();

    }
}