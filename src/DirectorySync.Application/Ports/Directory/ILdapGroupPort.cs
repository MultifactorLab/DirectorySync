using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Directory;

public interface ILdapGroupPort
{
    GroupModel? GetByGuidAsync(DirectoryGuid objectGuid);
    ReadOnlyCollection<GroupModel>? GetByGuidAsync(IEnumerable<DirectoryGuid> objectGuids);
}
