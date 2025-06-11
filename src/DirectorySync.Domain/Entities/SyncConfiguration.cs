using System.Collections.ObjectModel;
using DirectorySync.Domain.Karnel;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Entities;

public class SyncConfiguration : Entity
{
    private List<GroupMappingSettings> _groups;
    public ReadOnlyCollection<GroupMappingSettings> GroupMappingSettings => new(_groups);

    private SyncConfiguration(Guid id,
        IEnumerable<GroupMappingSettings> groupsMapping) : base(id)
    {
        ArgumentNullException.ThrowIfNull(nameof(groupsMapping));

        _groups = groupsMapping.ToList();
    }

    public static SyncConfiguration Create(Guid id, IEnumerable<GroupMappingSettings> groups)
    {
        return new SyncConfiguration(id, groups);
    }

    public void SetNewGroupMappingSettings(IEnumerable<GroupMappingSettings> groups)
    {
        _groups = groups.ToList();
    }
}
