namespace DirectorySync.Application.Workloads;

public interface IHandleNewUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}
