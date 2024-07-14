namespace DirectorySync.Application.Workloads;

public interface ISynchronizeUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
