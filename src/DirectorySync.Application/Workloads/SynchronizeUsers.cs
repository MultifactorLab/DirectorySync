using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

/// <summary>
/// Deletes and updates users in Multifactor Cloud.
/// </summary>
public interface ISynchronizeUsers
{
    Task ExecuteAsync(Guid groupGuid, Guid[] trackingGroups, CancellationToken token = default);
}

internal class SynchronizeUsers : ISynchronizeUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly IGetReferenceGroup _getReferenceGroup;
    private readonly IGetReferenceUser _getReferenceUser;
    private readonly IApplicationStorage _storage;
    private readonly Deleter _deleter;
    private readonly Updater _updater;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<SynchronizeUsers> _logger;

    public SynchronizeUsers(RequiredLdapAttributes requiredLdapAttributes,
        IGetReferenceGroup getReferenceGroup,
        IGetReferenceUser getReferenceUser,
        IApplicationStorage storage,
        Deleter deleter,
        Updater updater,
        CodeTimer codeTimer,
        ILogger<SynchronizeUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroup = getReferenceGroup;
        _getReferenceUser = getReferenceUser;
        _storage = storage;
        _deleter = deleter;
        _updater = updater;
        _codeTimer = codeTimer;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid groupGuid, Guid[] trackingGroups, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogInformation(ApplicationEvent.StartUserSynchronization, "Start users synchronization for group {group}", groupGuid);

        var names = _requiredLdapAttributes.GetNames().ToArray();
        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", names));

        var getGroupTimer = _codeTimer.Start("Get Reference Group");
        var referenceGroup = _getReferenceGroup.Execute(groupGuid, names);
        getGroupTimer.Stop();
        _logger.LogDebug("Reference group found: {Group:l}", referenceGroup);

        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug("Users synchronizing skipping because group doesn't exist in cache storage");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
            return;
        }

        var modifiedMembers = new List<ReferenceDirectoryUserUpdateModel>();

        if (ReferenceGroupHasDifferentCountOfMembers(referenceGroup, cachedGroup))
        {
            _logger.LogDebug("Reference and cached groups are different");
            _logger.LogDebug("Searching for deleted members...");
            var allGroups = _storage.FindGroups(trackingGroups.Select(c => new DirectoryGuid(c))).ToArray();
            var memberGroupMap = BuildMemberGroupMap(allGroups, cachedGroup);

            var groupUnlinkedGuids = GetUnlinkedGuids(referenceGroup, cachedGroup, memberGroupMap);

            var groupUnlinkedMembers = GetUnlinkedMembers(groupUnlinkedGuids, names, memberGroupMap);

            await HandleDeletedMembers(cachedGroup, referenceGroup, groupUnlinkedMembers, token);

            modifiedMembers.AddRange(groupUnlinkedMembers);
        }

        modifiedMembers.AddRange(GetModifiedMembers(referenceGroup, cachedGroup));

        if (modifiedMembers.Count == 0)
        {
            _logger.LogDebug("Modified members was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
            return;
        }

        _logger.LogDebug("Found modified users: {Modified}", modifiedMembers);
        await _updater.UpdateManyAsync(cachedGroup, modifiedMembers.ToArray(), token);

        var updateModifiedTimer = _codeTimer.Start("Update Cached Group: Modified Users");
        _storage.UpdateGroup(cachedGroup);
        updateModifiedTimer.Stop();
        _logger.LogDebug("Modified members are synchronized");

        _logger.LogInformation(ApplicationEvent.CompleteUsersSynchronization, "Complete users synchronization for group {group}", groupGuid);
    }

    private static Dictionary<DirectoryGuid, List<DirectoryGuid>> BuildMemberGroupMap(IEnumerable<CachedDirectoryGroup> allGroups, CachedDirectoryGroup cachedGroup)
    {
        return allGroups
            .SelectMany(g => g.Members.Select(m => (UserId: m.Id, GroupId: g.GroupGuid)))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).ToList());
    }

    private List<DirectoryGuid> GetUnlinkedGuids(ReferenceDirectoryGroup referenceGroup, CachedDirectoryGroup cachedGroup, Dictionary<DirectoryGuid, List<DirectoryGuid>> memberGroupMap)
    {
        var result = new List<DirectoryGuid>();

        var deletedFromGroup = GetDeletedMemberGuids(referenceGroup, cachedGroup).ToArray();

        foreach (var deletedMemberGuid in deletedFromGroup)
        {
            if (memberGroupMap.ContainsKey(deletedMemberGuid))
            {
                result.Add(deletedMemberGuid);
            }
        }

        return result;
    }

    private async Task HandleDeletedMembers(CachedDirectoryGroup cachedGroup, ReferenceDirectoryGroup referenceGroup, IEnumerable<ReferenceDirectoryUserUpdateModel> groupUnlinkedMembers, CancellationToken token)
    {
        var deletedFromGroup = GetDeletedMemberGuids(referenceGroup, cachedGroup).ToArray();
        var deletedMembers = deletedFromGroup
            .Intersect(groupUnlinkedMembers
                .Where(u => u.UserGroupIds.Count <= 1)
                .Select(u => u.Guid))
            .ToArray();

        if (deletedMembers.Length == 0)
        {
            _logger.LogDebug("Deleted members was not found");
            return;
        }

        _logger.LogDebug("Found deleted users: {Deleted}", deletedMembers.Length);
        await _deleter.DeleteManyAsync(cachedGroup, deletedMembers, token);
        _logger.LogDebug("Deleted members are synchronized");
    }

    private List<ReferenceDirectoryUserUpdateModel> GetModifiedMembers(ReferenceDirectoryGroup referenceGroup, CachedDirectoryGroup cachedGroup)
    {
        return MemberChangeDetector.GetModifiedMembers(referenceGroup, cachedGroup)
            .Select(ReferenceDirectoryUserUpdateModel.FromEntity)
            .ToList();
    }

    private static bool ReferenceGroupHasDifferentCountOfMembers(ReferenceDirectoryGroup refGroup, CachedDirectoryGroup cachedGroup)
    {
        var referenceMembersHash = EntriesHash.Create(refGroup.Members.Select(x => x.Guid));
        return referenceMembersHash != cachedGroup.Hash;
    }

    private static IEnumerable<DirectoryGuid> GetDeletedMemberGuids(ReferenceDirectoryGroup referenceGroup,
        CachedDirectoryGroup cachedGroup)
    {
        var refMemberGuids = referenceGroup.Members.Select(x => x.Guid);
        var cachedMemberGuids = cachedGroup.Members.Select(x => x.Id);
        return cachedMemberGuids.Except(refMemberGuids);
    }

    private IEnumerable<ReferenceDirectoryUserUpdateModel> GetUnlinkedMembers(IEnumerable<DirectoryGuid> groupUnlinkedMembers,
        string[] names,
        Dictionary<DirectoryGuid, List<DirectoryGuid>> memberGroupMap)
    {
        foreach (var memberGuid in groupUnlinkedMembers)
        {
            var refUser = _getReferenceUser.Execute(memberGuid, names);

            if (refUser != null)
            {
                memberGroupMap.TryGetValue(refUser.Guid, out var groupGuids);
                var modifiedMember = ReferenceDirectoryUserUpdateModel.FromEntity(refUser);
                modifiedMember.SetUserGroups(groupGuids?.Select(g => new DirectoryGuid(g)).ToList() ?? new List<DirectoryGuid>());
                modifiedMember.UnlinkFromGroup();
                yield return modifiedMember;
            }
        }
    }
}
