using DirectorySync.Application.Models.Entities;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class DirectoryGroupExtensions
{
    public static DirectoryGroupPersistenceModel ToPersistenceModel(this CachedDirectoryGroup entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var members = entity.Members.Select(x => x.ToPersistenceModel());
        return new DirectoryGroupPersistenceModel(entity.Guid, entity.Hash, members);
    }
}