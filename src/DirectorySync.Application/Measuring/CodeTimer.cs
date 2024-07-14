using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Measuring;

public sealed class CodeTimer
{
    private readonly ILoggerFactory _factory;
    private readonly MeasuringOptions _options;

    public CodeTimer(ILoggerFactory factory, IOptions<MeasuringOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public ICodeTimerScope Start(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        if (!_options.MeasureExecutionTime)
        {
            return new NullTimerScope();
        }

        var logger = _factory.CreateLogger("Measuring");
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return new NullTimerScope();
        }

        return new CodeTimerScope(name, _factory.CreateLogger("Measuring"));
    }
}
