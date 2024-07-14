using DirectorySync.Application.Extensions;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

internal class FakeScanUsers : IScanUsers
{
    private readonly ILogger<FakeScanUsers> _logger;

    public FakeScanUsers(ILogger<FakeScanUsers> logger)
    {
        _logger = logger;
    }
    
    public Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("Users scanning started");
        _logger.LogDebug("Users scanning complete");
        return Task.CompletedTask;
    }
}
