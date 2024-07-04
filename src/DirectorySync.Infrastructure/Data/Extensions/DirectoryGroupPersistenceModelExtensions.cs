using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using DirectorySync.Infrastructure.Data.Models;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class DirectoryGroupPersistenceModelExtensions
{
    public static CachedDirectoryGroup ToDomainModel(this DirectoryGroupPersistenceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        var guid = new DirectoryGuid(model.Id);
        var hash = new EntriesHash(model.Hash);
        var members = model.Members.Select(x => x.ToDomainModel());

        return new CachedDirectoryGroup(guid, members, hash);
    }
}