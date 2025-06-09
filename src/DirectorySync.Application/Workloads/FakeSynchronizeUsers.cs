using DirectorySync.Application.Extensions;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

internal class FakeSynchronizeUsers : ISynchronizeUsers
{
    private readonly ILogger<FakeSynchronizeUsers> _logger;

    public FakeSynchronizeUsers(ILogger<FakeSynchronizeUsers> logger)
    {
        _logger = logger;
    }
    
    public Task ExecuteAsync(Guid groupGuid, Guid[] trackingGroups, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("Users synchronization started");
        _logger.LogDebug("Users synchronization complete");
        return Task.CompletedTask;
    }
}
