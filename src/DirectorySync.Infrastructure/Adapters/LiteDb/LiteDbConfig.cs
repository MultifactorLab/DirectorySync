using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class LiteDbConfig
{
    [Required]
    public required string ConnectionString { get; set; }
}
