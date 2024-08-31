using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Measuring;

internal sealed class CodeTimerScope : ICodeTimerScope
{
    private bool _disposed;
    
    private readonly string? _name;
    private readonly ILogger? _logger;
    private readonly Stopwatch? _sw;

    public CodeTimerScope(string name, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        _name = name;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sw = Stopwatch.StartNew();
    }

    public void Stop()
    {
        if (!_disposed)
        {
            StopAndLog();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAndLog();
        }
    }

    private void StopAndLog()
    {
        if (_sw is not null)
        {
            _sw.Stop();
            _logger?.LogDebug("Timer '{TimerName:l}' value: {Time:l}", _name, _sw.Elapsed.ToString("c"));
        }
        _disposed = true;
    }
}
