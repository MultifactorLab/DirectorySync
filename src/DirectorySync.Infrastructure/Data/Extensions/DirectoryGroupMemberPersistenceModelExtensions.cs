using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class DirectoryGroupMemberPersistenceModelExtensions
{
    public static CachedDirectoryGroupMember ToDomainModel(this DirectoryGroupMemberPersistenceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        var guid = new DirectoryGuid(model.Guid);
        var identity = new MultifactorIdentity(model.Identity);
        var hash = new AttributesHash(model.Hash);
        return new CachedDirectoryGroupMember(guid, identity, hash);
    }
}
