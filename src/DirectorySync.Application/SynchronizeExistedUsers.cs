using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application;

public class SynchronizeExistedUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly GetReferenceGroupByGuid _getReferenceGroupByGuid;
    private readonly IApplicationStorage _storage;
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly ILogger<SynchronizeExistedUsers> _logger;

    public SynchronizeExistedUsers(RequiredLdapAttributes requiredLdapAttributes,
        GetReferenceGroupByGuid getReferenceGroupByGuid,
        IApplicationStorage storage,
        MultifactorPropertyMapper propertyMapper,
        IMultifactorApi api,
        ILogger<SynchronizeExistedUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroupByGuid = getReferenceGroupByGuid;
        _storage = storage;
        _propertyMapper = propertyMapper;
        _api = api;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        _logger.LogDebug("Users of group {group} synchronizing started", groupGuid);
        
        var names = _requiredLdapAttributes.GetNames().ToArray();
        var referenceGroup = _getReferenceGroupByGuid.Execute(groupGuid, names);
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug(
                "Users synchronizing skipping because group {group} doesn't exist in cache storage", 
                groupGuid);
            
            return;
        }

        using var withGroup = _logger.EnrichWithGroup(groupGuid);

        var referenceMembersHashe = new EntriesHash(referenceGroup.Members.Select(x => x.Guid));
        if (referenceMembersHashe != cachedGroup.Hash)
        {
            var deleted = GetDeletedMemberGuids(referenceGroup, cachedGroup).ToArray();
            if (deleted.Length == 0)
            {
                _logger.LogDebug("Nothing to delete");
            }
            else
            {
                _logger.LogDebug("Found deleted users: {Deleted}", deleted.Length);
                await DeleteAsync(cachedGroup, deleted, token);
            }
        }

        var modifiedMembers = MemberChangeDetector.GetModifiedMembers(referenceGroup, cachedGroup).ToArray();
        if (modifiedMembers.Length == 0)
        {
            _logger.LogDebug("Nothing to update");
            return;
        }

        await UpdateAsync(cachedGroup, modifiedMembers, token);
    }

    private static IEnumerable<DirectoryGuid> GetDeletedMemberGuids(ReferenceDirectoryGroup referenceGroup, 
        CachedDirectoryGroup cachedGroup)
    {
        var refMemberGuids = referenceGroup.Members.Select(x => x.Guid);
        var cachedMemberGuids = cachedGroup.Members.Select(x => x.Guid);

        return refMemberGuids.Except(cachedMemberGuids);
    }

    private async Task DeleteAsync(CachedDirectoryGroup group, 
        DirectoryGuid[] deletedUsers,
        CancellationToken token)
    {
        var mfIds = group.Members
            .Where(x => deletedUsers.Contains(x.Guid))
            .Select(x => x.UserId);

        await _api.DeleteManyAsync(mfIds, token);
        group.DeleteMembers(deletedUsers);
    }

    private async Task UpdateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryGroupMember[] modified,
        CancellationToken token)
    {
        var bucket = new UpdateBucket();

        foreach (ReferenceDirectoryGroupMember member in modified)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);
            
            var cachedMember = group.Members.First(x => x.Guid == member.Guid);
            var user = bucket.AddUser(cachedMember.UserId);

            var props = _propertyMapper.Map(member.Attributes);
            foreach (KeyValuePair<string,string?> prop in props)
            {
                user.AddProperty(prop.Key, prop.Value);
            }
        }

        await _api.UpdateManyAsync(bucket, token);
    }
}
