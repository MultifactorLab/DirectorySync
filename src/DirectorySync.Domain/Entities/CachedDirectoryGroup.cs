using System.Collections.ObjectModel;

namespace DirectorySync.Domain.Entities;

public class CachedDirectoryGroup : CachedDirectoryObject
{
    public EntriesHash Hash { get; private set; }

    private readonly List<CachedDirectoryGroupMember> _members;
    public ReadOnlyCollection<CachedDirectoryGroupMember> Members => new(_members);

    private bool _modified;
    public bool Modified => _modified || _members.Any(x => x.Modified);

    public CachedDirectoryGroup(DirectoryGuid guid, 
        IEnumerable<CachedDirectoryGroupMember> members, 
        EntriesHash hash)
    : base(guid)
    {
        ArgumentNullException.ThrowIfNull(members);
        _members = members.ToList();
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
    }

    public static CachedDirectoryGroup Create(DirectoryGuid guid, 
        IEnumerable<CachedDirectoryGroupMember> members)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(members);

        var membersArr = members.ToArray();
        var hash = new EntriesHash(membersArr.Select(x => x.Guid));
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
            var joined = $"Specified users already exist: {string.Join(", ", intersection.Select(x => x.Guid))}";
            throw new InvalidOperationException(joined);
        }

        _members.AddRange(members);
        UpdateHash();
        
        _modified = true;
    }
    
    public void DeleteMembers(params DirectoryGuid[] memberGuids)
    {
        ArgumentNullException.ThrowIfNull(memberGuids);

        if (memberGuids.Length == 0)
        {
            return;
        }

        _members.RemoveAll(x => memberGuids.Contains(x.Guid));
        UpdateHash();
        
        _modified = true;
    }
    
    public void Commit()
    {
        if (!Modified)
        {
            return;
        }
        _modified = false;
        foreach (var member in _members)
        {
            member.Commit();
        }
    }

    private void UpdateHash()
    {
        Hash = new EntriesHash(_members.Select(x => x.Guid));
    }

    public override string ToString() => Guid.Value.ToString();
}
