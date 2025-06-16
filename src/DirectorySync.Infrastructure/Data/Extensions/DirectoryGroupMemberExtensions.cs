using DirectorySync.Application.Models.Entities;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class DirectoryGroupMemberExtensions
{
    public static DirectoryGroupMemberPersistenceModel ToPersistenceModel(this CachedDirectoryGroupMember entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DirectoryGroupMemberPersistenceModel(entity.Id, entity.Identity, entity.Hash);
    }
}
