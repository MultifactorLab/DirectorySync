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
        var hash = new AttributesHash(model.Hash);
        var userId = new MultifactorUserId(model.UserId);
        return new CachedDirectoryGroupMember(guid, hash, userId);
    }
}
