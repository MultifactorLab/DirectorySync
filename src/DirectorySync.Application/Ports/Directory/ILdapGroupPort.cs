using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapGroupPort
{
    (GroupModel? groups, ReadOnlyCollection<LdapDomain> domains) GetByGuid(DirectoryGuid objectGuid);
    (ReadOnlyCollection<GroupModel> groups, ReadOnlyCollection<LdapDomain> domains) GetByGuid(IEnumerable<DirectoryGuid> objectGuids);
}
