using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapGroupPort
{
    GroupModel? GetByGuid(DirectoryGuid objectGuid);
    ReadOnlyCollection<GroupModel>? GetByGuid(IEnumerable<DirectoryGuid> objectGuids);
}
