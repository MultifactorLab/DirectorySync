using LiteDB;

namespace DirectorySync.Infrastructure.Data.Models;

internal class DirectoryGroupMemberPersistenceModel
{
    public Guid Guid { get; private set; }
    public string Hash { get; private set; }
    public string UserId { get; private set; }

    public DirectoryGroupMemberPersistenceModel(Guid guid, 
        string hash,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(hash));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
        }

        Guid = guid;
        Hash = hash;
        UserId = userId;
    }

    [BsonCtor]
    protected DirectoryGroupMemberPersistenceModel() { }
}
