using Serilog.Core;
using Serilog.Events;

namespace DirectorySync.Infrastructure.Logging.Enrichers;

internal class MfTraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MfTraceId", ActivityContext.Current.ActivityId));
    }
}
