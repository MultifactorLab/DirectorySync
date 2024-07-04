using DirectorySync.Application;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class UserSynchronizer : IHostedService, IAsyncDisposable
{
    private readonly SyncOptions _syncOptions;
    private readonly SynchronizeExistedUsers _synchronizeExistedUsers;
    private readonly WorkloadState _state;
    private readonly ILogger<UserSynchronizer> _logger;

    private readonly CancellationTokenSource _cts = new();
    private PeriodicTimer? _timer;

    public UserSynchronizer(IOptions<SyncOptions> syncOptions,
        SynchronizeExistedUsers synchronizeExistedUsers,
        WorkloadState state,
        ILogger<UserSynchronizer> logger)
    {
        _syncOptions = syncOptions.Value;
        _synchronizeExistedUsers = synchronizeExistedUsers;
        _state = state;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogInformation(ApplicationEvent.ApplicationStarted, 
            "{Service:l} is starting at {DateTime}",
            nameof(UserSynchronizer),
            DateTime.Now);
        
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
            "{Service:l} is topping at {DateTime}",
            nameof(UserSynchronizer),
            DateTime.Now);
        
        return Task.FromCanceled(_cts.Token);
    }
    
    private async Task DoWork()
    {
        while (await _timer!.WaitForNextTickAsync(_cts.Token))
        {
            if (_state.NewUserHandlePerforming || _state.UserSyncPerforming)
            {
                _logger.LogInformation(ApplicationEvent.UserSyncTimerSkipping, "Some service workloads are already performing, skipping");
                return;
            }
        
            _state.StartUserSync();
            
            foreach (var guid in _syncOptions.Groups)
            {
                await _synchronizeExistedUsers.ExecuteAsync(guid, _cts.Token);
            }
            
            _logger.LogInformation(ApplicationEvent.UserSyncTimerTriggered,
                "{Service:l} timer triggered, start of user synchronization",
                nameof(UserSynchronizer));
        
            _state.StopUserSync();
        }
    }

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _timer = null;
        return ValueTask.CompletedTask;
    }
}
