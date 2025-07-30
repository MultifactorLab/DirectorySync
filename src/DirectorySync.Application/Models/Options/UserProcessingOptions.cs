using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Models.Options;

public class UserProcessingOptions
{
    [Range(1, 100)]
    public int DeletingBatchSize { get; set; } = 50;

    [Range(1, 100)]
    public int CreatingBatchSize { get; set; } = 20;

    [Range(1, 100)]
    public int UpdatingBatchSize { get; set; } = 50;
    
    public TimeSpan RequestInterval { get; set; } = TimeSpan.FromMilliseconds(200);
}
