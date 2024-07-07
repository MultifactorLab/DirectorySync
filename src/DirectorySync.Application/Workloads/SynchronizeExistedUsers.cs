using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

public class SynchronizeExistedUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly GetReferenceGroupByGuid _getReferenceGroupByGuid;
    private readonly IApplicationStorage _storage;
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly ILogger<SynchronizeExistedUsers> _logger;

    internal SynchronizeExistedUsers(RequiredLdapAttributes requiredLdapAttributes,
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
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("Users synchronization started");
        
        var names = _requiredLdapAttributes.GetNames().ToArray();
        var referenceGroup = _getReferenceGroupByGuid.Execute(groupGuid, names);
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug("Users synchronizing skipping because group doesn't exist in cache storage");
            return;
        }

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
                _storage.UpdateGroup(cachedGroup);
            }
        }

        var modifiedMembers = MemberChangeDetector.GetModifiedMembers(referenceGroup, cachedGroup).ToArray();
        if (modifiedMembers.Length == 0)
        {
            _logger.LogDebug("Nothing to update");
            return;
        }

        await UpdateAsync(cachedGroup, modifiedMembers, token);
        _storage.UpdateGroup(cachedGroup);
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

        var bucket = new DeletedUsersBucket();
        foreach (var id in mfIds)
        {
            bucket.AddDeletedUser(id);
        }

        var res = await _api.DeleteManyAsync(bucket, token);
        
        foreach (var id in res.DeletedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.UserId == id);
            if (cachedUser is not null)
            {
                group.DeleteMembers(cachedUser.Guid);
            }
        }
    }

    private async Task UpdateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryGroupMember[] modified,
        CancellationToken token)
    {
        var bucket = new ModifiedUsersBucket();
        foreach (ReferenceDirectoryGroupMember member in modified)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);
            
            var props = _propertyMapper.Map(member.Attributes);
            
            var cachedMember = group.Members.First(x => x.Guid == member.Guid);
            var user = bucket.AddModifiedUser(cachedMember.UserId, props[MultifactorPropertyName.IdentityProperty]!);

            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }
        }

        var res = await _api.UpdateManyAsync(bucket, token);
        foreach (var id in res.UpdatedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.UserId == id);
            if (cachedUser is null)
            {
                continue;
            }
            
            var refMember = modified.First(x => x.Guid == cachedUser.Guid);
            var hash = new AttributesHash(refMember.Attributes);
            cachedUser.UpdateHash(hash);
        }
    }
}
