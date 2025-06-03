using DirectorySync.Domain.Entities;
using DirectorySync.Domain;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Models;

internal class CachedMembershipModel
{
    public ReadOnlyDictionary<DirectoryGuid, List<DirectoryGuid>> MemborshipMap { get; private set; }

    public static CachedMembershipModel BuildMemberGroupMap(IEnumerable<CachedDirectoryGroup> groups)
    {
        var map = groups
            .SelectMany(g => g.Members.Select(m => (UserId: m.Id, GroupId: g.GroupGuid)))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).ToList());

        return new CachedMembershipModel()
        {
            MemborshipMap = new ReadOnlyDictionary<DirectoryGuid, List<DirectoryGuid>>(map)
        };
    }
}

