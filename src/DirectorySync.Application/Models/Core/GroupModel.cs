using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public class GroupModel : BaseModel
{
    public EntriesHash MembersHash { get; private set; }
        
    private readonly List<DirectoryGuid> _memberIds = new();
    public ReadOnlyCollection<DirectoryGuid> MemberIds => _memberIds.AsReadOnly();
    
    public ChangeOperation Operation { get; private set; } = ChangeOperation.None;
        
    private GroupModel(DirectoryGuid id,
        IEnumerable<DirectoryGuid> members) : base(id)
    {
        ArgumentNullException.ThrowIfNull(members, nameof(members));
            
        _memberIds = members.ToList();
        RecalculateHash();
    }


    public static GroupModel Create(Guid guid,
        IEnumerable<DirectoryGuid> newMembers)
    {
        ArgumentNullException.ThrowIfNull(newMembers, nameof(newMembers));
        
        return new GroupModel(guid, newMembers);
    }

    public void AddMembers(IEnumerable<DirectoryGuid> newMemberIds)
    {
        ArgumentNullException.ThrowIfNull(newMemberIds);

        var intersection = _memberIds.Intersect(newMemberIds).ToArray();
        if (intersection.Length != 0)
        {
            var joined = $"Specified users already exist: {string.Join(", ", intersection.Select(x => x.Value))}";
            throw new InvalidOperationException(joined);
        }

        _memberIds.AddRange(newMemberIds);
        RecalculateHash();
    }

    public void RemoveMembers(IEnumerable<DirectoryGuid> memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        _memberIds.RemoveAll(x => memberIds.Contains(x));
        RecalculateHash();
    }

    private void RecalculateHash()
    {
        MembersHash = EntriesHash.Create(_memberIds);
    }
    
    public void MarkForUpdate() => Operation = ChangeOperation.Update;
    public void ResetOperation() => Operation = ChangeOperation.None;

    public override string ToString() => Id.ToString();
}
