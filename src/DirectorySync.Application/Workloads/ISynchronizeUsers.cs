namespace DirectorySync.Application.Workloads;

/// <summary>
/// Deletes and updates users in Multifactor Cloud.
/// </summary>
public interface ISynchronizeUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
