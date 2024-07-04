using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application;

internal static class MemberChangeDetector
{
    public static IEnumerable<ReferenceDirectoryGroupMember> GetModifiedMembers(ReferenceDirectoryGroup referenceGroup, 
        CachedDirectoryGroup cachedGroup)
    {
        ArgumentNullException.ThrowIfNull(referenceGroup);
        ArgumentNullException.ThrowIfNull(cachedGroup);

        var refMemberGuids = referenceGroup.Members.Select(x => x.Guid);
        var cachedMemberGuids = cachedGroup.Members.Select(x => x.Guid);

        foreach (DirectoryGuid guid in refMemberGuids.Intersect(cachedMemberGuids))
        {
            var referenceMember = referenceGroup.Members.First(x => x.Guid == guid);
            var cachedMember = cachedGroup.Members.First(x => x.Guid == guid);
            
            var referenceAttributesHash = new AttributesHash(referenceMember.Attributes);
            var cachedAttributesHash = cachedMember.Hash;
            
            if (referenceAttributesHash != cachedAttributesHash)
            {
                yield return referenceMember;
            }
        }
    }
}
