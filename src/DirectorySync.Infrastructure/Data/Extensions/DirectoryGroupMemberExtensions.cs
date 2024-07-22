using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class DirectoryGroupMemberExtensions
{
    public static DirectoryGroupMemberPersistenceModel ToPersistenceModel(this CachedDirectoryGroupMember entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DirectoryGroupMemberPersistenceModel(entity.Guid, entity.Hash, entity.Identity);
    }
}
