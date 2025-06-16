namespace DirectorySync.Application.Models
{
    public class GroupMapping
    {
        public string DirectoryGroup { get; set; } = string.Empty;
        public string[] SignUpGroups { get; set; } = [];
    }
}
