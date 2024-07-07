using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Data;

internal class LiteDbConfig
{
    [Required]
    public required string ConnectionString { get; set; }
}
