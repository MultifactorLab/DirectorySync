using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Directory;

namespace DirectorySync.Infrastructure.Adapters.Ldap;

public class LdapMember : ILdapMemberPort
{
    public Task<MemberModel?> GetByGuidAsync(DirectoryGuid objectGuid, string[] requiredAttributes, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ReadOnlyCollection<MemberModel>> GetByGuidsAsync(IEnumerable<DirectoryGuid> objectGuids, string[] requiredAttributes, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
