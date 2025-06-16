using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Entities;

public class CachedDirectoryGroup : CachedDirectoryObject
{
    public DirectoryGuid GroupGuid { get; set; }
    public EntriesHash Hash { get; private set; }

    private readonly List<CachedDirectoryGroupMember> _members;
    public ReadOnlyCollection<CachedDirectoryGroupMember> Members => new(_members);

    public CachedDirectoryGroup(DirectoryGuid guid, 
        IEnumerable<CachedDirectoryGroupMember> members, 
        EntriesHash hash)
    : base(guid)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(members);

        GroupGuid = guid;
        _members = members.ToList();
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
    }

    public static CachedDirectoryGroup Create(DirectoryGuid guid, 
        IEnumerable<CachedDirectoryGroupMember> members)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(members);

        var membersArr = members.ToArray();
        var hash = EntriesHash.Create(membersArr.Select(x => x.Id));
        return new CachedDirectoryGroup(guid, membersArr, hash);
    }
    
    public void AddMembers(params CachedDirectoryGroupMember[] members)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (members.Length == 0)
        {
            return;
        }

        var intersection = _members.Intersect(members).ToArray();
        if (intersection.Length != 0)
        {
            var joined = $"Specified users already exist: {string.Join(", ", intersection.Select(x => x.Id))}";
            throw new InvalidOperationException(joined);
        }

        _members.AddRange(members);
        UpdateHash();
    }
    
    public void DeleteMembers(params DirectoryGuid[] memberGuids)
    {
        ArgumentNullException.ThrowIfNull(memberGuids);

        if (memberGuids.Length == 0)
        {
            return;
        }

        _members.RemoveAll(x => memberGuids.Contains(x.Id));
        UpdateHash();
    }

    private void UpdateHash()
    {
        Hash = EntriesHash.Create(_members.Select(x => x.Id));
    }

    public override string ToString() => Guid.Value.ToString();
}
