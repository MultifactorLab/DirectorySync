using LiteDB;

namespace DirectorySync.Infrastructure.Data.Models;

internal class DirectoryGroupPersistenceModel
{
    [BsonId]
    public Guid Id { get; private set; }
    public string Hash { get; private set; }
    
    public List<DirectoryGroupMemberPersistenceModel> Members { get; private set; }
    
    public DirectoryGroupPersistenceModel(Guid id,
        string hash,
        IEnumerable<DirectoryGroupMemberPersistenceModel> members)
    {
        Id = id;
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        
        ArgumentNullException.ThrowIfNull(members);
        Members = members.ToList();
    }

    [BsonCtor]
    protected DirectoryGroupPersistenceModel() { }
}