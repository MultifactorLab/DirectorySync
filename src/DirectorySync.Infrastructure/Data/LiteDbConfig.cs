using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Data;

public class LiteDbConfig
{
    [Required]
    public required string ConnectionString { get; set; }
}
