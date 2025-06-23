using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using LiteDB;

namespace DirectorySync.Infrastructure.Dto.LiteDb;

public class GroupPersistenceModel
{
    [BsonId]
    public Guid Id { get; private set; }
    public string Hash { get; private set; }
    
    public List<Guid> MemberIds { get; private set; }
    
    public GroupPersistenceModel(Guid id,
        EntriesHash hash,
        IEnumerable<Guid> members)
    {
        Id = id;

        ArgumentNullException.ThrowIfNull(hash);
        Hash = hash.Value;
        
        ArgumentNullException.ThrowIfNull(members);
        MemberIds = members.ToList();
    }

    [BsonCtor]
    protected GroupPersistenceModel() { }

    public static GroupPersistenceModel FromDomainModel(GroupModel model)
    {
        return new GroupPersistenceModel(model.Id.Value, model.MembersHash, model.MemberIds.Select(id => id.Value));
    }

    public static GroupModel ToDomainModel(GroupPersistenceModel dbModel)
    {
        return GroupModel.Create(dbModel.Id, dbModel.MemberIds.Select(c => new DirectoryGuid(c)));
    }
}
