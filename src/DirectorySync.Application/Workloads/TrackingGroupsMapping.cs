using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Domain;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal class TrackingGroupsMapping
{
    private readonly object _locker = new();
    private Dictionary<DirectoryGuid, string[]> _directoryGroupMappings = new();

    public TrackingGroupsMapping(IOptionsMonitor<GroupMappingsOptions> options)
    {
        _directoryGroupMappings = Map(options.CurrentValue);

        options.OnChange(newOptions =>
        {
            lock (_locker)
            {
                _directoryGroupMappings = Map(newOptions);
            }
        });
    }

    public Dictionary<DirectoryGuid, string[]> GetGroupsMapping()
    {
        lock (_locker)
        {
            return _directoryGroupMappings;
        }
    }

    private Dictionary<DirectoryGuid, string[]> Map(GroupMappingsOptions options)
    {
        return options.DirectoryGroupMappings
            .ToDictionary(
                c => new DirectoryGuid(Guid.Parse(c.DirectoryGroup)),
                c => c.SignUpGroups.ToArray()
            )
            ?? new();
    }
}
