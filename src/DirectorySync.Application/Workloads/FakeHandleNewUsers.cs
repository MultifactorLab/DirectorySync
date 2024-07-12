using DirectorySync.Application.Extensions;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

internal class FakeHandleNewUsers : IHandleNewUsers
{
    private readonly ILogger<FakeHandleNewUsers> _logger;

    public FakeHandleNewUsers(ILogger<FakeHandleNewUsers> logger)
    {
        _logger = logger;
    }
    
    public Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("New users handling started");
        _logger.LogDebug("New users handling complete");
        return Task.CompletedTask;
    }
}
