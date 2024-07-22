using LiteDB;

namespace DirectorySync.Infrastructure.Data.Models;

internal class DirectoryGroupMemberPersistenceModel
{
    public Guid Guid { get; private set; }
    public string Identity { get; private set; }
    public string Hash { get; private set; }

    public DirectoryGroupMemberPersistenceModel(Guid guid, 
        string identity,
        string hash)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(hash));
        }

        Guid = guid;
        Identity = identity;
        Hash = hash;
    }

    [BsonCtor]
    protected DirectoryGroupMemberPersistenceModel() { }
}
