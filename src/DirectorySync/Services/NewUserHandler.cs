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
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        return Task.CompletedTask;
        
        _timer = new PeriodicTimer(_syncOptions.NewUserHandleTimer);
        _ = Task.Run(DoWork, _cts.Token);
        
        _logger.LogInformation(ApplicationEvent.NewUserHandleStarted, "{Service:l} is started", nameof(NewUserHandler));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        _cts.Cancel();
        
        _logger.LogInformation(ApplicationEvent.NewUserHandleStopping, "{Service:l} is stopping", nameof(NewUserHandler));
        
        return Task.FromCanceled(_cts.Token);
    }
    
    private async Task DoWork()
    {
        while (await _timer!.WaitForNextTickAsync(_cts.Token))
        {
            if (_workloads.IsBusy())
            {
                _logger.LogInformation(ApplicationEvent.NewUserHandleTimerSkipping, "Some service workloads are already performing, skipping");
                continue;
            }
        
            _workloads.Add(Workload.HandleNew);
            
            foreach (var guid in _syncOptions.Groups)
            {
                await _handleNewUsers.ExecuteAsync(guid, _cts.Token);
            }
        
            _logger.LogInformation(ApplicationEvent.NewUserHandleTimerTriggered,
                "{Service:l} timer triggered, start of new user handle",
                nameof(NewUserHandler));
        
            _workloads.Complete(Workload.HandleNew);
        }
    }

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _timer = null;
        return ValueTask.CompletedTask;
    }
}
