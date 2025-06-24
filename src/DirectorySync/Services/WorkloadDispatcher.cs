using DirectorySync.Application;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.UseCases;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.ConfigurationSources.Cloud;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class WorkloadDispatcher : IHostedService, IAsyncDisposable
{
    private readonly OrderBoard _board;
    private readonly ISynchronizeUsersUseCase _synchronizeUsers;
    private readonly ISynchronizeGroupsUseCase _synchronizeGroups;
    private readonly IInitialSynchronizeUsersUseCase _initialSynchronizeUsers;
    private readonly ISynchronizeCloudSettingsUseCase _synchronizeCloudSettings;
    private readonly ISyncSettingsCloudPort _syncSettingsCloudPort;
    private readonly CodeTimer _timer;
    private readonly IOptionsMonitor<SyncSettings> _syncSettings;
    private readonly ILogger<WorkloadDispatcher> _logger;
    
    private readonly CancellationTokenSource _cts = new();
    private Timer? _syncUsersTimer;
    private Timer? _syncGroupsTimer;
    private Timer? _syncSettingsTimer;
    private Task? _task;

    public WorkloadDispatcher(OrderBoard board,
        IOptionsMonitor<SyncSettings> syncSettings,
        ISynchronizeUsersUseCase synchronizeUsers,
        ISynchronizeGroupsUseCase synchronizeGroups,
        ISynchronizeCloudSettingsUseCase synchronizeCloudSettings,
        IInitialSynchronizeUsersUseCase initialSynchronizeUsers,
        ISyncSettingsCloudPort syncSettingsCloudPort,
        CodeTimer timer,
        ILogger<WorkloadDispatcher> logger)
    {
        _board = board;
        _synchronizeUsers = synchronizeUsers;
        _synchronizeGroups = synchronizeGroups;
        _initialSynchronizeUsers = initialSynchronizeUsers;
        _synchronizeCloudSettings = synchronizeCloudSettings;
        _syncSettingsCloudPort = syncSettingsCloudPort;
        _timer = timer;
        _logger = logger;
        _syncSettings = syncSettings;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        InitialSync(cancellationToken).Wait(cancellationToken);

        SetTimers(_syncSettings.CurrentValue);
        _task = Task.Run(ProcessWorkloads, _cts.Token);
        _syncSettings.OnChange(SetTimers);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAny(
            _task ?? Task.CompletedTask,
            Task.Run(() => { }, cancellationToken));
    }

    private void SetTimers(SyncSettings options)
    {
        _syncGroupsTimer = new Timer(_ => _board.Place(WorkloadKind.SynchronizeGroups), null, TimeSpan.Zero, options.ScanTimer);
        _syncUsersTimer = new Timer(_ => _board.Place(WorkloadKind.SynchronizeUsers), null, TimeSpan.Zero, options.SyncTimer);
        _syncSettingsTimer = new Timer(_ => _board.Place(WorkloadKind.SynchronizeSettings), null, TimeSpan.Zero, options.CloudConfigRefreshTimer);
    }
    
    private async Task ProcessWorkloads()
    {
        await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
        while (!_cts.IsCancellationRequested)
        {
            if (!_syncSettings.CurrentValue.ScanEnabled)
            {
                _board.Done(WorkloadKind.SynchronizeGroups);
            }

            if (!_syncSettings.CurrentValue.SyncEnabled)
            {
                _board.Done(WorkloadKind.SynchronizeUsers);
            }

            if (!_syncSettings.CurrentValue.SyncSettingsEnabled)
            {
                _board.Done(WorkloadKind.SynchronizeSettings);
            }

            var workload = _board.Take();
            switch (workload)
            {
                case WorkloadKind.SynchronizeUsers:
                    if (!_syncSettings.CurrentValue.SyncEnabled)
                    {
                        _board.Done(WorkloadKind.SynchronizeUsers);
                        break;
                    }

                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await SyncUsers();
                    break;

                case WorkloadKind.SynchronizeGroups:
                    if (!_syncSettings.CurrentValue.ScanEnabled)
                    {
                        _board.Done(WorkloadKind.SynchronizeGroups);
                        break;
                    } 

                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await SyncGroups();
                    break;
                
                case WorkloadKind.SynchronizeSettings:
                    if (!_syncSettings.CurrentValue.SyncSettingsEnabled)
                    {
                        _board.Done(WorkloadKind.SynchronizeSettings);
                        break;
                    } 

                    ActivityContext.Create(Guid.NewGuid().ToString());
                    await SyncSettings();
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
        _logger.LogDebug("Start of users synchronization");
        
        var timer = _timer.Start($"Users synchronization: Total");
        
        try
        {
            await _synchronizeUsers.ExecuteAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ApplicationEvent.UserSynchronizationServiceError, ex, "Error occured while synchronizing users. Details: {0}", ex.Message);
        }
        
        timer.Stop();
                    
        _board.Done(WorkloadKind.SynchronizeUsers);
        _logger.LogDebug("End of users synchronization");
    }
    
    private async Task SyncGroups()
    {
        _logger.LogDebug("Start of user scanning");
        
        var trackingGroups = _syncSettings.CurrentValue.TrackingGroups;
        
        var timer = _timer.Start($"Groups {String.Join(", ", trackingGroups.Select(c => c.Value))}");
                    
        try
        {
            await _synchronizeGroups.ExecuteAsync(trackingGroups, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ApplicationEvent.UserScanningServiceError, ex, "Error occured while  synchronizing groups. Details: {0}", ex.Message);
        }
            
        timer.Stop();
                    
        _board.Done(WorkloadKind.SynchronizeGroups);
        _logger.LogDebug("End of groups synchronization");
    }

    private async Task SyncSettings()
    {
        _logger.LogDebug("Start of settings synchronization");
        
        var timer = _timer.Start($"Settings synchronization: Total");

        try
        {
            var provider = CloudConfigurationSource.CurrentProvider;

            if (provider is null)
            {
                _logger.LogWarning("No cloud settings provider configured");
            }
            
            await _synchronizeCloudSettings.ExecuteAsync(false, provider, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ApplicationEvent.CloudSettingSynchronizationServiceError, ex, "Error occured while  synchronizing cloud settings. Details: {0}", ex.Message);
        }
        
        timer.Stop();
                    
        _board.Done(WorkloadKind.SynchronizeSettings);
        _logger.LogDebug("End of cloud settings synchronization");
    }

    private async Task InitialSync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Start of initial cloud settings synchronization");
            var provider = CloudConfigurationSource.CurrentProvider;
            
            if (provider is null)
            {
                _logger.LogWarning("No cloud settings provider configured");
            }
            
            provider.Init(_syncSettingsCloudPort);
            
            await _synchronizeCloudSettings.ExecuteAsync(true, provider, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ApplicationEvent.CloudSettingSynchronizationServiceError, "End of initial cloud users synchronization. Details: {0}", ex.Message);
        }
        
        var trackingGroups = _syncSettings.CurrentValue.TrackingGroups;

        try
        {
            _logger.LogDebug("Start of intial cloud users synchronization");
            await _initialSynchronizeUsers.ExecuteAsync(trackingGroups, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ApplicationEvent.CloudSynchronizationServiceError, "End of initial cloud users synchronization. Details: {0}", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_syncUsersTimer is IAsyncDisposable syncUsersTimer)
        {
            _logger.LogInformation(ApplicationEvent.UserSynchronizationServiceStopping, "SYNC is now stopping");
            await syncUsersTimer.DisposeAsync();
        }
        _syncUsersTimer = null;
        
        if (_syncGroupsTimer is IAsyncDisposable syncGroupsTimer)
        {
            _logger.LogInformation(ApplicationEvent.UserScanningServiceStopping, "SCAN is now stopping");
            await syncGroupsTimer.DisposeAsync();
        }
        _syncGroupsTimer = null;

        if (_syncSettingsTimer is IAsyncDisposable syncSettingsTimer)
        {
            _logger.LogInformation(ApplicationEvent.CloudSettingSynchronizationServiceStopping, "SCAN is now stopping");
            await syncSettingsTimer.DisposeAsync();
        }
        _syncSettingsTimer = null;
    }
}
