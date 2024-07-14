namespace DirectorySync.Application.Workloads;

public interface IScanUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
