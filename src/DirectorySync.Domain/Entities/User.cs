using System.Collections.ObjectModel;
using DirectorySync.Domain.Karnel;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Entities;

public class User : Entity
{
    public Identity Identity { get; }
    public AttributesHash Hash { get; private set; }
    private readonly List<Guid> _groupIds;
    public ReadOnlyCollection<Guid> GroupIds => new(_groupIds);

    private User(Guid id,
        Identity identity,
        AttributesHash hash,
        IEnumerable<Guid> groupIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        ArgumentNullException.ThrowIfNull(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(hash, nameof(hash));
        ArgumentNullException.ThrowIfNull(groupIds, nameof(groupIds));

        Id = id;
        Identity = identity;
        Hash = hash;
        _groupIds = groupIds.ToList();
    }

    public static User Create(Guid id,
        Identity identity,
        AttributesHash attributesHash,
        IEnumerable<Guid> groupIds)
    {
        return new User(id, identity, attributesHash, groupIds);
    }

    public void AddGroups(Guid[] groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);

        if (groupIds.Length == 0)
        {
            return;
        }

        var intersection = _groupIds.Intersect(groupIds).ToArray();
        if (intersection.Length != 0)
        {
            var joined = $"Specified users already exist: {string.Join(", ", intersection)}";
            throw new InvalidOperationException(joined);
        }

        _groupIds.AddRange(groupIds);
        //UpdateHash();
    }

    public void DeleteGroups(Guid[] groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);

        if (groupIds.Length == 0)
        {
            return;
        }

        _groupIds.RemoveAll(x => groupIds.Contains(x));
        //UpdateHash();
    }

    public void UpdateHash(AttributesHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (Hash != hash)
        {
            Hash = hash;
        }
    }
}

