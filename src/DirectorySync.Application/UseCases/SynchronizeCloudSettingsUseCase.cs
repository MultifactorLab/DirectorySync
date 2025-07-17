using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.ConfigurationProviders;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.UseCases;

public interface ISynchronizeCloudSettingsUseCase
{
    Task ExecuteAsync(bool isInit, ICloudConfigurationProvider provider, CancellationToken cancellationToken = default);
}

public class SynchronizeCloudSettingsUseCase : ISynchronizeCloudSettingsUseCase
{
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly ISyncSettingsDatabase _syncSettingsDatabase;
    private readonly IMemberDatabase _memberDatabase;
    private readonly IGroupDatabase _groupDatabase;
    private readonly IUserGroupsMapper _userGroupsMapper;
    private readonly IUserUpdater _userUpdater;
    private readonly IUserDeleter _userDeleter;
    private readonly IGroupUpdater _groupUpdater;
    private readonly ILogger<SynchronizeCloudSettingsUseCase> _logger;

    public SynchronizeCloudSettingsUseCase(ISyncSettingsOptions syncSettingsOptions,
        ISyncSettingsDatabase syncSettingsDatabase,
        IMemberDatabase memberDatabase,
        IGroupDatabase groupDatabase,
        IUserGroupsMapper userGroupsMapper,
        IUserUpdater userUpdater,
        IUserDeleter userDeleter,
        IGroupUpdater groupUpdater,
        ILogger<SynchronizeCloudSettingsUseCase> logger)
    {
        _syncSettingsOptions = syncSettingsOptions;
        _syncSettingsDatabase = syncSettingsDatabase;
        _memberDatabase = memberDatabase;
        _groupDatabase = groupDatabase;
        _userGroupsMapper = userGroupsMapper;
        _userUpdater = userUpdater;
        _userDeleter = userDeleter;
        _groupUpdater = groupUpdater;
        _logger = logger;
    }

    public async Task ExecuteAsync(bool isInit,
        ICloudConfigurationProvider provider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Start cloud settings synchronization");

        SyncSettings? currentSyncSettings;
        
        currentSyncSettings = isInit ? _syncSettingsDatabase.GetSyncSettings() : _syncSettingsOptions.Current;
        
        provider.Load();
        var newSyncSettings = _syncSettingsOptions.Current;

        if (currentSyncSettings is null)
        {
            _logger.LogInformation("Cloud settings synchronized");
            _syncSettingsDatabase.SaveSettings(newSyncSettings);
            return;
        }

        if (AreEqual(currentSyncSettings.DirectoryGroupMappings, newSyncSettings.DirectoryGroupMappings))
        {
            _logger.LogInformation("DirectoryGroupMappings did not change");
            return;
        }

        var affectedGroupIds = GetChangedGroups(currentSyncSettings.DirectoryGroupMappings, newSyncSettings.DirectoryGroupMappings);

        var affectedMembers = GetAffectedMembers(affectedGroupIds);

        var oldMap = currentSyncSettings.DirectoryGroupMappings
            .ToDictionary(
                kpv => new DirectoryGuid(Guid.Parse(kpv.DirectoryGroup)),
                kpv => kpv.SignUpGroups.ToArray()
            );
        var newMap = newSyncSettings.DirectoryGroupMappings
            .ToDictionary(
                kpv => new DirectoryGuid(Guid.Parse(kpv.DirectoryGroup)),
                kpv => kpv.SignUpGroups.ToArray()
            );

        var groupsToRemove = oldMap.Select(c => c.Key).Except(newMap.Select(c => c.Key));
        
        foreach (var member in affectedMembers)
        {
            member.RemoveGroups(groupsToRemove);
            
            var(toAdd, toRemove) = _userGroupsMapper.SetUserCloudGroupsDiff(member, oldMap, newMap);
            
            member.AddCloudGroups(toAdd);
            member.RemoveCloudGroups(toRemove);

            if (member.GroupIds.Count == 0)
            {
                member.MarkForDelete();
            }
            else if (member.AddedCloudGroups.Count > 0 || member.RemovedCloudGroups.Count > 0)
            {
                member.MarkForUpdate();
            }
        }
        
        var toUpdate = affectedMembers
            .Where(m => m.Operation == ChangeOperation.Update)
            .ToList();
        var toDelete = affectedMembers
            .Where(m => m.Operation == ChangeOperation.Delete)
            .ToList();
        
        ReadOnlyCollection<MemberModel> updated = ReadOnlyCollection<MemberModel>.Empty;
        ReadOnlyCollection<MemberModel> deleted = ReadOnlyCollection<MemberModel>.Empty;
        
        if (toUpdate.Count != 0)
        {
            updated = await _userUpdater.UpdateManyAsync(toUpdate, cancellationToken);
        }

        if (toDelete.Count != 0)
        {
            deleted = await _userDeleter.DeleteManyAsync(toDelete, cancellationToken);
        }
        
        _memberDatabase.UpdateMany(affectedMembers);
        _groupUpdater.UpdateGroupsWithMembers(updated.Concat(deleted));
        
        _groupDatabase.DeleteMany(groupsToRemove);
        
        _syncSettingsDatabase.SaveSettings(newSyncSettings);
        _logger.LogInformation(ApplicationEvent.CompleteCloudSettingSynchronization, "Complete cloud settings synchronization");
    }

    private ReadOnlyCollection<MemberModel> GetAffectedMembers(IEnumerable<DirectoryGuid> affectedGroupIds)
    {
        var affectedGroups = _groupDatabase.FindById(affectedGroupIds);
        var affectedMemberIds = affectedGroups.SelectMany(g => g.MemberIds);
        return _memberDatabase.FindManyById(affectedMemberIds);
    }
    
    private bool AreEqual(GroupMapping[] oldMappings, GroupMapping[] newMappings)
    {
        return oldMappings.Length == newMappings.Length &&
               oldMappings.All(o => newMappings.Any(n => n.DirectoryGroup == o.DirectoryGroup &&
                                                         n.SignUpGroups.OrderBy(x => x).SequenceEqual(o.SignUpGroups.OrderBy(x => x))));
    }
    
    private HashSet<DirectoryGuid> GetChangedGroups(GroupMapping[] oldMappings, GroupMapping[] newMappings)
    {
        var oldMap = oldMappings.ToDictionary(m => m.DirectoryGroup, m => m.SignUpGroups);
        var newMap = newMappings.ToDictionary(m => m.DirectoryGroup, m => m.SignUpGroups);

        var changedGroups = new HashSet<DirectoryGuid>();

        foreach (var key in oldMap.Keys.Union(newMap.Keys))
        {
            oldMap.TryGetValue(key, out var oldValues);
            newMap.TryGetValue(key, out var newValues);

            if (!Enumerable.SequenceEqual(
                    oldValues ?? Array.Empty<string>(), 
                    newValues ?? Array.Empty<string>()))
            {
                changedGroups.Add(new DirectoryGuid(Guid.Parse(key)));
            }
        }

        return changedGroups;
    }
}
