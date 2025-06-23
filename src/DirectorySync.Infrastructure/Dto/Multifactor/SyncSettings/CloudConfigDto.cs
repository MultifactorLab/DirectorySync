using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Infrastructure.Dto.Cloud.SyncSettings;

namespace DirectorySync.Infrastructure.Dto.Multifactor.SyncSettings
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


        public static Application.Models.Options.SyncSettings ToModel(CloudConfigDto dto)
        {
            return new Application.Models.Options.SyncSettings
            {
                Enabled = dto.Enabled,
                CloudConfigRefreshTimer = dto.CloudConfigRefreshTimer,
                SyncTimer = dto.SyncTimer,
                ScanTimer = dto.ScanTimer,
                TimeoutAd = dto.TimeoutAD,
                TrackingGroups = dto.DirectoryGroupMappings.Select(d => new DirectoryGuid(Guid.Parse(d.DirectoryGroup))).ToArray(),
                DirectoryGroupMappings = dto.DirectoryGroupMappings.Select(GroupMappingsDto.ToModel).ToArray(),
                IncludeNestedGroups = dto.IncludeNestedGroups,
                PropertyMapping = PropsMappingDto.ToModel(dto.PropertyMapping)
            };
        }
    }
}
