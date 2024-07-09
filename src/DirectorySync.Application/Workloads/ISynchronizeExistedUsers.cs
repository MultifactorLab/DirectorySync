namespace DirectorySync.Application.Workloads;

public interface ISynchronizeExistedUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
