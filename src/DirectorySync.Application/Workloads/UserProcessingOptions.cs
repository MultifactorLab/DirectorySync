using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Workloads;

public class UserProcessingOptions
{
    [Range(1, 500)]
    public int DeletingBatchSize { get; set; } = 50;

    [Range(1, 200)]
    public int CreatingBatchSize { get; set; } = 20;

    [Range(1, 300)]
    public int UpdatingBatchSize { get; set; } = 50;
}
