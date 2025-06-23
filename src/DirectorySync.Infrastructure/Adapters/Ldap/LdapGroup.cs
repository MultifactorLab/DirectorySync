using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

public class LdapGroup : ILdapGroupPort
{
    public Task<GroupModel?> GetByGuidAsync(DirectoryGuid objectGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ReadOnlyCollection<GroupModel>?> GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
