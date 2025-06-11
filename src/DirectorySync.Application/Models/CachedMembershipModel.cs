using DirectorySync.Domain.Entities;
using System.Collections.ObjectModel;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Application.Models;

internal class CachedMembershipModel
{
    public ReadOnlyDictionary<DirectoryGuid, DirectoryGuid[]> MembershipMap { get; private set; }

    public static CachedMembershipModel BuildMemberGroupMap(IEnumerable<CachedDirectoryGroup> groups)
    {
        var map = groups
            .SelectMany(g => g.Members.Select(m => (UserId: m.Id, GroupId: g.GroupGuid)))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).ToArray());

        return new CachedMembershipModel()
        {
            MembershipMap = new ReadOnlyDictionary<DirectoryGuid, DirectoryGuid[]>(map)
        };
    }
}
