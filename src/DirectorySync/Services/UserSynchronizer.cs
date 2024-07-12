using DirectorySync.Application;
using DirectorySync.Application.Workloads;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class UserSynchronizer : IHostedService, IAsyncDisposable
{
    private readonly SyncOptions _syncOptions;
    private readonly ISynchronizeExistedUsers _synchronizeExistedUsers;
    private readonly WorkloadsTasks _workloads;
    private readonly ILogger<UserSynchronizer> _logger;

    private readonly CancellationTokenSource _cts = new();
    private PeriodicTimer? _timer;
    private Task? _task;

    public UserSynchronizer(IOptions<SyncOptions> syncOptions,
        ISynchronizeExistedUsers synchronizeExistedUsers,
        WorkloadsTasks workloads,
        ILogger<UserSynchronizer> logger)
    {
        _syncOptions = syncOptions.Value;
        _synchronizeExistedUsers = synchronizeExistedUsers;
        _workloads = workloads;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.SyncEnabled)
        {
            _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStartedDisabled, "{Service:l} is started but disabled", nameof(UserSynchronizer));
            return Task.CompletedTask;
        }
        
        _timer = new PeriodicTimer(_syncOptions.SyncTimer);
        _task = Task.Run(DoWork, _cts.Token);
        
        _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStarted, "{Service:l} is started", nameof(UserSynchronizer));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.SyncEnabled)
        {
            return Task.CompletedTask;
        }
        
        _cts.Cancel();
        
        _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStopping, "{Service:l} is stopping", nameof(UserSynchronizer));
        
        return Task.WhenAny(
            _task ?? Task.CompletedTask,
            Task.Run(() => { }, cancellationToken));
    }
    
    private async Task DoWork()
    {
        await Task.Delay(5000);
        do
        {
            try
            {
                if (_workloads.IsBusy())
                {
                    _logger.LogDebug("Some service workloads are already performing, skipping");
                    continue;
                }

                _logger.LogDebug("{Service:l} timer triggered, start of user synchronization", nameof(UserSynchronizer));
                _workloads.Add(Workload.Synchronize);

                foreach (var guid in _syncOptions.Groups)
                {
                    await _synchronizeExistedUsers.ExecuteAsync(guid, _cts.Token);
                }

                _workloads.Complete(Workload.Synchronize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ApplicationEvent.UserSynchronizationServiceError, ex, "Error occured while synchronizing users");
            }
        } while (await _timer!.WaitForNextTickAsync(_cts.Token));
    }

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _timer = null;
        return ValueTask.CompletedTask;
    }
}
