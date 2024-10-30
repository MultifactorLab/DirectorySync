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
    private readonly SyncOptions _syncOptions;
    private readonly ILogger<WorkloadDispatcher> _logger;
    
    private readonly CancellationTokenSource _cts = new();
    private Timer? _syncTimer;
    private Timer? _scanTimer;
    private Task? _task;

    public WorkloadDispatcher(OrderBoard board,
        IOptions<SyncOptions> syncOptions,
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
        _syncOptions = syncOptions.Value;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        
        if (_syncOptions.ScanEnabled)
        {
            _scanTimer = new Timer(_ => _board.Place(WorkloadKind.Scan), null, TimeSpan.Zero, _syncOptions.ScanTimer);
            _logger.LogInformation(ApplicationEvent.UserScanningServiceStarted, "SCAN is started");
        }
        else
        {
            _logger.LogInformation(ApplicationEvent.UserScanningServiceStartedDisabled, "SCAN is started but disabled");
        }
        
        if (_syncOptions.SyncEnabled)
        {
            _syncTimer = new Timer(_ => _board.Place(WorkloadKind.Synchronize), null, TimeSpan.Zero, _syncOptions.SyncTimer);
            _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStarted, "SYNC is started");
        }
        else
        {
            _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStartedDisabled, "SYNC is started but disabled");
        }

        if (_syncOptions.SyncEnabled || _syncOptions.ScanEnabled)
        {
            _task = Task.Run(ProcessWorkloads, _cts.Token);
        }
        else
        {
            _task = Task.CompletedTask;
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAny(
            _task ?? Task.CompletedTask,
            Task.Run(() => { }, cancellationToken));
    }
    
    private async Task ProcessWorkloads()
    {
        await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
        while (!_cts.IsCancellationRequested)
        {
            var workload = _board.Take();
            if (workload == WorkloadKind.Empty)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }

            switch (workload)
            {
                case WorkloadKind.Synchronize:
                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await SyncUsers();
                    break;
                
                case WorkloadKind.Scan:
                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await ScanUsers();
                    break;
                
                default:
                    _logger.LogDebug("Unknown workload kind, skipping...");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    break;
            }
        }
    }

    private async Task SyncUsers()
    {
        _logger.LogDebug("Start of user synchronization");
                    
        foreach (var guid in _syncOptions.Groups)
        {
            var timer = _timer.Start($"Group {guid} Sync: Total");

            try
            {
                await _synchronizeUsers.ExecuteAsync(guid, _cts.Token);
            }
            catch (IdentityAttributeNotDefinedException)
            {
                _logger.LogError(ApplicationEvent.InvalidServiceConfiguration, "Identity attribute mapping should be specified");
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
                    
        foreach (var guid in _syncOptions.Groups)
        {
            var timer = _timer.Start($"Group {guid} Scan: Total");

            try
            {
                await _scanUsers.ExecuteAsync(guid, _cts.Token);
            }
            catch (IdentityAttributeNotDefinedException)
            {
                _logger.LogError(ApplicationEvent.InvalidServiceConfiguration, "Identity attribute mapping should be specified");
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
