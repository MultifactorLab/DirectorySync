using DirectorySync.Application;
using Microsoft.Extensions.Options;

namespace DirectorySync.Services;

internal class NewUserHandler : IHostedService, IAsyncDisposable
{
    private readonly SyncOptions _syncOptions;
    private readonly WorkloadState _state;
    private readonly ILogger<NewUserHandler> _logger;

    private Timer? _timer;
    
    public NewUserHandler(IOptions<SyncOptions> syncOptions,
        WorkloadState state,
        ILogger<NewUserHandler> logger)
    {
        _syncOptions = syncOptions.Value;
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
            nameof(NewUserHandler),
            DateTime.Now);
        
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _syncOptions.NewUserHandleTimer);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_syncOptions.Enabled)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogInformation(ApplicationEvent.NewUserHandleStopping, 
            "{Service:l} is topping at {DateTime}",
            nameof(NewUserHandler),
            DateTime.Now);
        
        _timer?.Change(Timeout.Infinite, 0);
        
        return Task.CompletedTask;
    }
    
    private void DoWork(object? state)
    {
        if (_state.NewUserHandlePerforming || _state.UserSyncPerforming)
        {
            _logger.LogInformation(ApplicationEvent.NewUserHandleTimerSkipping, "Some service workloads are already performing, skipping");
            return;
        }
        
        _state.StartNewUserHandle();
        
        _logger.LogInformation(ApplicationEvent.NewUserHandleTimerTriggered,
            "{Service:l} timer triggered, start of new user handle",
            nameof(NewUserHandler));
        
        _state.StopNewUserHandle();
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }

        _timer = null;    
    }
}
