using DirectorySync.Application.Extensions;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

internal class FakeSynchronizeExistedUsers : ISynchronizeExistedUsers
{
    private readonly ILogger<FakeSynchronizeExistedUsers> _logger;

    public FakeSynchronizeExistedUsers(ILogger<FakeSynchronizeExistedUsers> logger)
    {
        _logger = logger;
    }
    
    public Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("Users synchronization started");
        _logger.LogDebug("Users synchronization complete");
        return Task.CompletedTask;
    }
}
