using System.Collections.ObjectModel;
using DirectorySync.Domain.Karnel;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Entities;

public class Group : Entity
{
    public EntriesHash Hash { get; private set; }
    private readonly List<Guid> _memberIds;
    public ReadOnlyCollection<Guid> MemberIds => new(_memberIds);

    private Group(Guid id,
        EntriesHash hash,
        IEnumerable<Guid> members) : base(id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        ArgumentNullException.ThrowIfNull(hash, nameof(hash));
        ArgumentNullException.ThrowIfNull(members, nameof(members));

        Id = id;
        Hash = hash;
        _memberIds = members.ToList();
    }


    public static Group Create(Guid guid,
        IEnumerable<Guid> members)
    {
        var hash = EntriesHash.Create(members);

        return new Group(guid, hash, members);
    }

    public void AddMembers(Guid[] memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        if (memberIds.Length == 0)
        {
            return;
        }

        var intersection = _memberIds.Intersect(memberIds).ToArray();
        if (intersection.Length != 0)
        {
            var joined = $"Specified users already exist: {string.Join(", ", intersection)}";
            throw new InvalidOperationException(joined);
        }

        _memberIds.AddRange(memberIds);
        UpdateHash();
    }

    public void DeleteMembers(Guid[] memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        if (memberIds.Length == 0)
        {
            return;
        }

        _memberIds.RemoveAll(x => memberIds.Contains(x));
        UpdateHash();
    }

    private void UpdateHash()
    {
        Hash = EntriesHash.Create(_memberIds);
    }

    public override string ToString() => Id.ToString();
}
