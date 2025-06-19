using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapMemberPort
{
    Task<MemberModel?> GetByGuidAsync(DirectoryGuid objectGuid, string[] requiredAttributes, CancellationToken cancellationToken = default);
    Task<ReadOnlyCollection<MemberModel>> GetByGuidsAsync(IEnumerable<DirectoryGuid> objectGuids, string[] requiredAttributes, CancellationToken cancellationToken = default);
}
