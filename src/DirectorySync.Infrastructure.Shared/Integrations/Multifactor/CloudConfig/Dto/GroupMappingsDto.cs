
namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto
{
    public class GroupMappingsDto
    {
        public string DirectoryGroup { get; set; }
        public string[] SignUpGroups { get; set; }
    }
}
