using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Integrations.Ldap.Extensions;

internal static class ReferenceGroupExtensions
{
    public static CachedDirectoryGroup ToCachedDirectoryGroup(this ReferenceDirectoryGroup refGroup)
    {
        ArgumentNullException.ThrowIfNull(refGroup);
        var members = refGroup.Members.Select(x => CachedDirectoryGroupMember.Create(x.Guid, x.Attributes));
        return CachedDirectoryGroup.Create(refGroup.Guid, members);
    }
}
