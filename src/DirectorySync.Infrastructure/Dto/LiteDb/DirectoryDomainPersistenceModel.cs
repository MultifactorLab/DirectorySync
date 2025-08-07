using LiteDB;

namespace DirectorySync.Infrastructure.Dto.LiteDb;

public class DirectoryDomainPersistenceModel
{
    public string Domain { get; set; }

    [BsonCtor]
    public DirectoryDomainPersistenceModel() { }
}
