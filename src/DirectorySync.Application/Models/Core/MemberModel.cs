using System.Collections.ObjectModel;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public class MemberModel : BaseModel
{
    public Identity Identity { get; }
    public AttributesHash AttributesHash { get; private set; }
    
    private readonly HashSet<MemberProperty> _memberProperties = new HashSet<MemberProperty>();
    public ReadOnlyCollection<MemberProperty> Properties { get;  private set; }
    
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
        IEnumerable<DirectoryGuid> groupIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(groupIds, nameof(groupIds));
        
        Identity = identity;
        _groupIds = groupIds.ToList();
    }

    private MemberModel(DirectoryGuid id,
        Identity identity,
        AttributesHash attributesHash,
        IEnumerable<DirectoryGuid> groupIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(attributesHash, nameof(attributesHash));
        ArgumentNullException.ThrowIfNull(groupIds, nameof(groupIds));
        
        Identity = identity;
        AttributesHash = attributesHash;
        _groupIds = groupIds.ToList();
    }

    public static MemberModel Create(Guid id,
        Identity identity,
        IEnumerable<DirectoryGuid> groupIds)
    {
        
        
        return new MemberModel(id, identity, groupIds);
    }
    
    public static MemberModel Create(Guid id,
        Identity identity,
        AttributesHash attributesHash,
        IEnumerable<DirectoryGuid> groupIds)
    {
        return new MemberModel(id, identity, attributesHash, groupIds);
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

    public void SetProperties(IEnumerable<MemberProperty> newProperties, AttributesHash newHash)
    {
        ArgumentNullException.ThrowIfNull(newProperties);

        if (AttributesHash == newHash)
        {
            return;
        }
        
        _memberProperties.Clear();
        foreach (var property in newProperties)
        {
            _memberProperties.Add(property);
        }
        AttributesHash = newHash;
    }

    public void SetProperties(LdapAttributeCollection newAttributes, LdapAttributeMappingOptions options)
    {
        ArgumentNullException.ThrowIfNull(newAttributes);
        
        var newHash = new AttributesHash(newAttributes);

        if (AttributesHash == newHash)
        {
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(options.NameAttribute))
        {
            var name = newAttributes.GetFirstOrDefault(options.NameAttribute);
            if (name is not null)
            {
                _memberProperties.Add(new MemberProperty(MemberPropertyOptions.AdditionalProperties.NameProperty, name));
            }
        }

        var email = newAttributes.GetFirstOrDefault(options.EmailAttributes);
        if (email is not null)
        {
            _memberProperties.Add(new MemberProperty(MemberPropertyOptions.AdditionalProperties.EmailProperty, email));
        }

        var phone = newAttributes.GetFirstOrDefault(options.PhoneAttributes);
        if (phone is not null)
        {
            _memberProperties.Add(new MemberProperty(MemberPropertyOptions.AdditionalProperties.PhoneProperty, phone));
        }
        
        AttributesHash = newHash;
    }
    
    public void MarkForCreate() => Operation = ChangeOperation.Create;
    public void MarkForUpdate() => Operation = ChangeOperation.Update;
    public void MarkForDelete() => Operation = ChangeOperation.Delete;
    public void ResetOperation() => Operation = ChangeOperation.None;
}
