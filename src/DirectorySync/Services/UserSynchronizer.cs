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
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogInformation(ApplicationEvent.UserSyncStarted, 
            "{Service:l} is starting at {DateTime}",
            nameof(UserSynchronizer),
            DateTime.Now);
        
        return Task.CompletedTask;
        
        _timer = new PeriodicTimer(_syncOptions.SyncTimer);
        _ = Task.Run(DoWork, _cts.Token);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        _cts.Cancel();
        
        _logger.LogInformation(ApplicationEvent.UserSyncStopping, 
            "{Service:l} is stopping at {DateTime}",
            nameof(UserSynchronizer),
            DateTime.Now);
        
        return Task.FromCanceled(_cts.Token);
    }
    
    private async Task DoWork()
    {
        while (await _timer!.WaitForNextTickAsync(_cts.Token))
        {
            if (_workloads.IsBusy())
            {
                _logger.LogInformation(ApplicationEvent.UserSyncTimerSkipping, "Some service workloads are already performing, skipping");
                continue;
            }
        
            _workloads.Add(Workload.Synchronize);
            
            foreach (var guid in _syncOptions.Groups)
            {
                await _synchronizeExistedUsers.ExecuteAsync(guid, _cts.Token);
            }
            
            _logger.LogInformation(ApplicationEvent.UserSyncTimerTriggered,
                "{Service:l} timer triggered, start of user synchronization",
                nameof(UserSynchronizer));
        
            _workloads.Complete(Workload.Synchronize);
        }
    }

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _timer = null;
        return ValueTask.CompletedTask;
    }
}
