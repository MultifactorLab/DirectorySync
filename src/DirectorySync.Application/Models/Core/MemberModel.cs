using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public class MemberModel : BaseModel
{
    public Identity Identity { get; }
    
    public LdapAttributeCollection Attributes { get;  private set; }
    public AttributesHash AttributesHash { get; private set; }
    
    private readonly List<DirectoryGuid> _groupIds = new();
    public ReadOnlyCollection<DirectoryGuid> GroupIds => _groupIds.AsReadOnly();
    
    public ChangeOperation Operation { get; private set; } = ChangeOperation.None;
    
    private readonly List<DirectoryGuid> _removedGroupIds = new();
    private readonly List<DirectoryGuid> _addedGroupIds = new();
    
    public ReadOnlyCollection<DirectoryGuid> RemovedGroupIds => _removedGroupIds.AsReadOnly();
    public ReadOnlyCollection<DirectoryGuid> AddedGroupIds => _addedGroupIds.AsReadOnly();
    
    private readonly List<string> _removedCloudGroups = new();
    private readonly List<string> _addedCloudGroups = new();
    
    public ReadOnlyCollection<string> RemovedCloudGroups => _removedCloudGroups.AsReadOnly();
    public ReadOnlyCollection<string> AddedCloudGroups => _addedCloudGroups.AsReadOnly();
    

    private MemberModel(DirectoryGuid id,
        Identity identity,
        LdapAttributeCollection attributes,
        IEnumerable<DirectoryGuid> groupIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(attributes, nameof(attributes));
        ArgumentNullException.ThrowIfNull(groupIds, nameof(groupIds));
        
        Identity = identity;
        Attributes = attributes;
        AttributesHash = new AttributesHash(attributes);
        _groupIds = groupIds.ToList();
    }

    public static MemberModel Create(Guid id,
        Identity identity,
        LdapAttributeCollection attributes,
        IEnumerable<DirectoryGuid> groupIds)
    {
        return new MemberModel(id, identity, attributes, groupIds);
    }

    public void AddGroups(IEnumerable<DirectoryGuid> groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);

        var duplicates  = _groupIds.Intersect(groupIds).ToArray();
        if (duplicates.Length != 0)
        {
            var joined = $"Groups already assigned: {string.Join(", ", duplicates.Select(d => d.Value))}";
            throw new InvalidOperationException(joined);
        }

        _groupIds.AddRange(groupIds);
        _addedGroupIds.AddRange(groupIds);
    }

    public void RemoveGroups(IEnumerable<DirectoryGuid> groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);

        _groupIds.RemoveAll(x => groupIds.Contains(x));
        _removedGroupIds.AddRange(groupIds);
    }

    public void AddCloudGroups(IEnumerable<string> cloudGroups)
    {
        ArgumentNullException.ThrowIfNull(cloudGroups);
        
        _addedCloudGroups.AddRange(cloudGroups);
    }

    public void RemoveCloudGroups(IEnumerable<string> cloudGroups)
    {
        ArgumentNullException.ThrowIfNull(cloudGroups);
        
        _removedCloudGroups.AddRange(cloudGroups);
    }

    public void SetNewAttributes(LdapAttributeCollection newAttributes)
    {
        ArgumentNullException.ThrowIfNull(newAttributes);
        
        var newHash = new AttributesHash(newAttributes);

        if (AttributesHash != newHash)
        {
            Attributes = newAttributes;
            AttributesHash = newHash;
        }
    }
    
    public void MarkForCreate() => Operation = ChangeOperation.Create;
    public void MarkForUpdate() => Operation = ChangeOperation.Update;
    public void MarkForDelete() => Operation = ChangeOperation.Delete;
    public void ResetOperation() => Operation = ChangeOperation.None;
}
