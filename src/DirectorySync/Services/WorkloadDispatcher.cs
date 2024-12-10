using DirectorySync.Application;
using DirectorySync.Application.Exceptions;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Workloads;
using DirectorySync.Infrastructure;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class WorkloadDispatcher : IHostedService, IAsyncDisposable
{
    private readonly OrderBoard _board;
    private readonly ISynchronizeUsers _synchronizeUsers;
    private readonly IScanUsers _scanUsers;
    private readonly CodeTimer _timer;
    private readonly IOptionsMonitor<SyncOptions> _syncOptions;
    private readonly ILogger<WorkloadDispatcher> _logger;
    
    private readonly CancellationTokenSource _cts = new();
    private Timer? _syncTimer;
    private Timer? _scanTimer;
    private Task? _task;

    public WorkloadDispatcher(OrderBoard board,
        IOptionsMonitor<SyncOptions> syncOptions,
        ISynchronizeUsers synchronizeUsers,
        IScanUsers scanUsers,
        CodeTimer timer,
        ILogger<WorkloadDispatcher> logger)
    {
        _board = board;
        _synchronizeUsers = synchronizeUsers;
        _scanUsers = scanUsers;
        _timer = timer;
        _logger = logger;
        _syncOptions = syncOptions;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        SetTimers(_syncOptions.CurrentValue);
        _task = Task.Run(ProcessWorkloads, _cts.Token);
        _syncOptions.OnChange(SetTimers);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAny(
            _task ?? Task.CompletedTask,
            Task.Run(() => { }, cancellationToken));
    }

    private void SetTimers(SyncOptions options)
    {
        _scanTimer = new Timer(_ => _board.Place(WorkloadKind.Scan), null, TimeSpan.Zero, options.ScanTimer);
        _syncTimer = new Timer(_ => _board.Place(WorkloadKind.Synchronize), null, TimeSpan.Zero, options.SyncTimer);
    }
    
    private async Task ProcessWorkloads()
    {
        await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
        while (!_cts.IsCancellationRequested)
        {
            if (!_syncOptions.CurrentValue.ScanEnabled)
            {
                _board.Done(WorkloadKind.Scan);
            }            
            
            if (!_syncOptions.CurrentValue.SyncEnabled)
            {
                _board.Done(WorkloadKind.Synchronize);
            }

            var workload = _board.Take();
            switch (workload)
            {
                case WorkloadKind.Synchronize:
                    if (!_syncOptions.CurrentValue.SyncEnabled) 
                    {
                        _board.Done(WorkloadKind.Synchronize);
                        break;
                    }

                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await SyncUsers();
                    break;
                
                case WorkloadKind.Scan:
                    if (!_syncOptions.CurrentValue.ScanEnabled)
                    {
                        _board.Done(WorkloadKind.Scan);
                        break;
                    }

                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await ScanUsers();
                    break;

                case WorkloadKind.Empty:
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    break;

                default:
                    _logger.LogDebug("Unknown workload kind: {Workload}", workload);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    break;
            }
        }
    }

    private async Task SyncUsers()
    {
        _logger.LogDebug("Start of user synchronization");
                    
        foreach (var guid in _syncOptions.CurrentValue.Groups)
        {
            var timer = _timer.Start($"Group {guid} Sync: Total");

            try
            {
                await _synchronizeUsers.ExecuteAsync(guid, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ApplicationEvent.UserSynchronizationServiceError, ex, "Error occured while synchronizing users");
            }
            
            timer.Stop();
        }
                    
        _board.Done(WorkloadKind.Synchronize);
        _logger.LogDebug("End of user synchronization");
    }
    
    private async Task ScanUsers()
    {
        _logger.LogDebug("Start of user scanning");
                    
        foreach (var guid in _syncOptions.CurrentValue.Groups)
        {
            var timer = _timer.Start($"Group {guid} Scan: Total");

            try
            {
                await _scanUsers.ExecuteAsync(guid, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ApplicationEvent.UserScanningServiceError, ex, "Error occured while scanning users");
            }
            
            timer.Stop();
        }
                    
        _board.Done(WorkloadKind.Scan);
        _logger.LogDebug("End of user scanning");
    }

    public async ValueTask DisposeAsync()
    {
        if (_syncTimer is IAsyncDisposable syncTimer)
        {
            _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStopping, "SYNC is now stopping");
            await syncTimer.DisposeAsync();
        }
        _syncTimer = null;
        
        if (_scanTimer is IAsyncDisposable scanTimer)
        {
            _logger.LogInformation(ApplicationEvent.UserScanningServiceStopping, "SCAN is now stopping");
            await scanTimer.DisposeAsync();
        }
        _scanTimer = null;
    }
}
