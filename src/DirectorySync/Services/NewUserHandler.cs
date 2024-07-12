using DirectorySync.Application;
using DirectorySync.Application.Workloads;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class NewUserHandler : IHostedService, IAsyncDisposable
{
    private readonly SyncOptions _syncOptions;
    private readonly WorkloadsTasks _workloads;
    private readonly IHandleNewUsers _handleNewUsers;
    private readonly ILogger<NewUserHandler> _logger;

    private readonly CancellationTokenSource _cts = new();
    private PeriodicTimer? _timer;
    private Task? _task;
    
    public NewUserHandler(IOptions<SyncOptions> syncOptions,
        WorkloadsTasks workloads,
        IHandleNewUsers handleNewUsers,
        ILogger<NewUserHandler> logger)
    {
        _syncOptions = syncOptions.Value;
        _workloads = workloads;
        _handleNewUsers = handleNewUsers;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.NewUserHandleEnabled)
        {
            _logger.LogInformation(ApplicationEvent.NewUserHandlingServiceStartedDisabled, "{Service:l} is started but disabled", nameof(NewUserHandler));
            return Task.CompletedTask;
        }
        
        _timer = new PeriodicTimer(_syncOptions.NewUserHandleTimer);
        _task = Task.Run(DoWork, _cts.Token);
        
        _logger.LogInformation(ApplicationEvent.NewUserHandlingServiceStarted, "{Service:l} is started", nameof(NewUserHandler));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.NewUserHandleEnabled)
        {
            return Task.CompletedTask;
        }
        
        _cts.Cancel();
        
        _logger.LogInformation(ApplicationEvent.NewUserHandlingServiceStopping, "{Service:l} is stopping", nameof(NewUserHandler));
        
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

                _logger.LogDebug("{Service:l} timer triggered, start of new user handle", nameof(NewUserHandler));
                _workloads.Add(Workload.HandleNew);

                foreach (var guid in _syncOptions.Groups)
                {
                    await _handleNewUsers.ExecuteAsync(guid, _cts.Token);
                }

                _workloads.Complete(Workload.HandleNew);
            }
            catch (Exception ex)
            {
                _logger.LogError(ApplicationEvent.NewUserHandlingServiceError, ex, "Error occured while handling new users");
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
