using Serilog.Events;
using Serilog.Sinks.EventLog;

namespace DirectorySync.Infrastructure.Logging;

internal class DirectorySyncEventIdProvider : IEventIdProvider
{
    public ushort ComputeEventId(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out var eventId))
        {
            return 0;
        }

        if (eventId is ScalarValue scalar)
        {
            return ushort.TryParse(scalar.Value?.ToString(), out var parsed1) 
                ? parsed1 
                : (ushort)0;
        }
        
        if (eventId is StructureValue structure)
        {
            return ushort.TryParse( structure.Properties[0].Value.ToString(), out var parsed2) 
                ? parsed2 
                : (ushort)0;
        }

        return 0;
    }
}
