using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;

public class LiteDbConfig
{
    [Required]
    public required string ConnectionString { get; set; }
}
