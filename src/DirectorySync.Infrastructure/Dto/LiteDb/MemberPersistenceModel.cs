using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using LiteDB;

namespace DirectorySync.Infrastructure.Dto.LiteDb;

public class MemberPersistenceModel
{
    [BsonId]
    public Guid Id { get; private set; }
    public string Identity { get; private set; }
    public string Hash { get; private set; }
    public Guid[] GroupIds { get; private set; }

    public MemberPersistenceModel(Guid id, 
        string identity,
        string hash,
        IEnumerable<Guid> groups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(groups, nameof(groups));

        Id = id;
        Identity = identity;
        Hash = hash;
        GroupIds = groups.ToArray();
    }

    [BsonCtor]
    protected MemberPersistenceModel() { }
    
    public static MemberPersistenceModel FromDomainModel(MemberModel model)
    {
        return new MemberPersistenceModel(model.Id.Value,
            model.Identity.Value,
            model.AttributesHash.Value,
            model.GroupIds.Select(id => id.Value));
    }

    public static MemberModel ToDomainModel(MemberPersistenceModel dbModel)
    {
        return MemberModel.Create(dbModel.Id,
            new Identity(dbModel.Identity),
            new AttributesHash(dbModel.Hash),
            dbModel.GroupIds.Select(g => new DirectoryGuid(g)));
    }
}
