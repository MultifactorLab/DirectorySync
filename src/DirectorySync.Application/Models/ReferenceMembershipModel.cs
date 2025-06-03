using System.Collections.ObjectModel;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Enums;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Models;

internal class ReferenceMembershipModel
{
    public ReadOnlyDictionary<MultifactorIdentity, List<DirectoryGuid>> MemborshipMap { get; private set; }

    public static ReferenceMembershipModel BuildMemberGroupMap(IEnumerable<ReferenceDirectoryGroup> groups,
        LdapAttributeMappingOptions options,
        UserNameFormat userNameFormat)
    {
        var map = groups
            .SelectMany(g => g.Members
            .Select(m => (Identity: FormatIdentity(m.Attributes.GetSingleOrDefault(options.IdentityAttribute), userNameFormat), GroupId: g.Guid)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Identity))
            .GroupBy(x => x.Identity)
            .ToDictionary(g => g.Key!, g => g.Select(x => x.GroupId).ToList());

        return new ReferenceMembershipModel()
        {
            MemborshipMap = new ReadOnlyDictionary<MultifactorIdentity, List<DirectoryGuid>>(map)
        };
    }

    private static MultifactorIdentity FormatIdentity(string identity, UserNameFormat userNameFormat)
    {
        return userNameFormat switch
        {
            UserNameFormat.Identity => MultifactorIdentity.FromRawString(identity),
            UserNameFormat.ActiveDirectory => MultifactorIdentity.FromLdapFormat(identity),
            _ => throw new NotImplementedException(userNameFormat.ToString())
        };
    }
}
