using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
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
    private readonly ILogger<SynchronizeCloudSettingsUseCase> _logger;

    public SynchronizeCloudSettingsUseCase(ISyncSettingsOptions syncSettingsOptions,
        ISyncSettingsDatabase syncSettingsDatabase,
        IMemberDatabase memberDatabase,
        IGroupDatabase groupDatabase,
        IUserGroupsMapper userGroupsMapper,
        ILogger<SynchronizeCloudSettingsUseCase> logger)
    {
        _syncSettingsOptions = syncSettingsOptions;
        _syncSettingsDatabase = syncSettingsDatabase;
        _memberDatabase = memberDatabase;
        _groupDatabase = groupDatabase;
        _userGroupsMapper = userGroupsMapper;
        _logger = logger;
    }

    public async Task ExecuteAsync(bool isInit,
        ICloudConfigurationProvider provider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Start cloud settings synchronization");

        SyncSettings currentSyncSettings;
        
        if (isInit)
        {
            currentSyncSettings = _syncSettingsDatabase.GetSyncSettings();
        }
        else
        {
            currentSyncSettings = _syncSettingsOptions.Current;
        }
        
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

        foreach (var member in affectedMembers)
        {
            var(toAdd, toRemove) = _userGroupsMapper.SetUserCloudGroupsDiff(member, oldMap, newMap);
            
            member.AddCloudGroups(toAdd);
            member.RemoveCloudGroups(toRemove);
        }

        _syncSettingsDatabase.SaveSettings(newSyncSettings);
        _memberDatabase.InsertMany(affectedMembers);
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
