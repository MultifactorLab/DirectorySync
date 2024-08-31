namespace DirectorySync.Application.Workloads;

/// <summary>
/// Creates users in Multifactor Cloud.
/// </summary>
public interface IScanUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
