using DirectorySync.Domain.ValueObjects;
using LiteDB;

namespace DirectorySync.Infrastructure.Data.Models;

internal class DirectoryGroupMemberPersistenceModel
{
    public Guid Guid { get; private set; }
    public string Identity { get; private set; }
    public string Hash { get; private set; }

    public DirectoryGroupMemberPersistenceModel(Guid guid, 
        string identity,
        AttributesHash hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(hash);

        Guid = guid;
        Identity = identity;
        Hash = hash.Value;
    }

    [BsonCtor]
    protected DirectoryGroupMemberPersistenceModel() { }
}
