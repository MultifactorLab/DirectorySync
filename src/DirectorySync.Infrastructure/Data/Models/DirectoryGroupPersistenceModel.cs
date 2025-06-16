using DirectorySync.Domain.ValueObjects;
using LiteDB;

namespace DirectorySync.Infrastructure.Data.Models;

internal class DirectoryGroupPersistenceModel
{
    [BsonId]
    public Guid Id { get; private set; }
    public string Hash { get; private set; }
    
    public List<DirectoryGroupMemberPersistenceModel> Members { get; private set; }
    
    public DirectoryGroupPersistenceModel(Guid id,
        EntriesHash hash,
        IEnumerable<DirectoryGroupMemberPersistenceModel> members)
    {
        Id = id;

        ArgumentNullException.ThrowIfNull(hash);
        Hash = hash.Value;
        
        ArgumentNullException.ThrowIfNull(members);
        Members = members.ToList();
    }

    [BsonCtor]
    protected DirectoryGroupPersistenceModel() { }
}
