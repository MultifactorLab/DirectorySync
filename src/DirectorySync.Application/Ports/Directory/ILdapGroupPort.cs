using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapGroupPort
{
    Task<GroupModel?> GetByGuidAsync(DirectoryGuid objectGuid, CancellationToken cancellationToken = default);
    Task<ReadOnlyCollection<GroupModel>?> GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids, CancellationToken cancellationToken = default);
}
