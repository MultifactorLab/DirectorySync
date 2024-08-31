using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Workloads;

public class UserProcessingOptions
{
    [Range(1, 500)]
    public int DeletingPortionSize { get; set; } = 50;

    [Range(1, 200)]
    public int CreatingPortionSize { get; set; } = 20;

    [Range(1, 300)]
    public int UpdatingPortionSize { get; set; } = 50;
}
