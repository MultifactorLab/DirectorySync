using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapMemberPort
{
    ReadOnlyCollection<MemberModel> GetByGuids(IEnumerable<DirectoryGuid> objectGuids, string[] requiredAttributes, CancellationToken cancellationToken = default);
}
