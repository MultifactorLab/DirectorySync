using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.UseCases;

public interface IInitialSynchronizeUsersUseCase
{
    Task ExecuteAsync(IEnumerable<DirectoryGuid> trackingGroupGuids, CancellationToken token = default);
}

public class InitialSynchronizeUsersUseCase
{
    private readonly ISystemDatabase _systemDatabase;
    private readonly ILdapGroupPort _ldapGroupPort;
    private readonly ILdapMemberPort _ldapMemberPort;
    private readonly IUserCloudPort _userCloudPort;
    private readonly IUserDeleter _userDeleter;
    private readonly ISyncSettingsOptions _syncSettingsOptions;
    private readonly ILogger<InitialSynchronizeUsersUseCase> _logger;

    public InitialSynchronizeUsersUseCase(ISystemDatabase systemDatabase,
       ILdapGroupPort ldapGroupPort,
       ILdapMemberPort ldapMemberPort,
       IUserCloudPort userCloudPort,
       IUserDeleter userDeleter,
       ISyncSettingsOptions syncSettingsOptions,
       ILogger<InitialSynchronizeUsersUseCase> logger)
    {
       _systemDatabase = systemDatabase;
       _ldapGroupPort = ldapGroupPort;
       _ldapMemberPort = ldapMemberPort;
       _userCloudPort = userCloudPort;
       _userDeleter = userDeleter;
       _syncSettingsOptions = syncSettingsOptions;
       _logger = logger;
    }

    public async Task ExecuteAsync(ReadOnlyCollection<DirectoryGuid> trackingGroupGuids, CancellationToken cancellationToken = default)
    {
       if (trackingGroupGuids.Count == 0)
       {
           _logger.LogDebug("No tracking groups provided, skipping synchronization");
           throw new InvalidOperationException("No tracking groups provided");
       }

       _logger.LogDebug("Tracking group GUIDs: {GroupGUIDs}", string.Join(", ", trackingGroupGuids.Select(g => g.Value)));

       if (_systemDatabase.IsDatabaseInitialized())
       {
           _logger.LogDebug("Local storage already exists, skipping initial synchronization");
           return;
       }

       var cloudIdentities = await _userCloudPort.GetUsersIdentitesAsync(cancellationToken);
       _logger.LogDebug("Fetched {Count} identities from cloud",
           cloudIdentities.Count);

       var requiredAttributes = _syncSettingsOptions.GetRequiredAttributeNames();
       _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", requiredAttributes));

       var refIdentitiesMap = await GetTrackingReferenceMembers(trackingGroupGuids, requiredAttributes, cancellationToken);
       
       if (refIdentitiesMap.Count == 0)
       {
           _logger.LogWarning("No reference members found for given tracking groups");
           return;
       }

       var toDelete = GetDeletedMembersIdentities(cloudIdentities, refIdentitiesMap);
       
       await HandleDeletedMembers(toDelete.ToList().AsReadOnly(), cancellationToken);
    } 

    private async Task<HashSet<Identity>> GetTrackingReferenceMembers(ReadOnlyCollection<DirectoryGuid> trackingGroups,
        string[] requiredAttributes,
        CancellationToken cancellationToken = default)
    {
       var referenceGroups = await _ldapGroupPort.GetByGuidAsync(trackingGroups);
       
       if (referenceGroups is null || referenceGroups.Count == 0)
       {
           _logger.LogWarning("No reference groups found for given tracking groups");
           return new HashSet<Identity>();
       }
       
       var members = new List<MemberModel>();

       foreach (var referenceGroup in referenceGroups)
       {
           members.AddRange(await _ldapMemberPort.GetByGuidsAsync(referenceGroup.MemberIds));
       }
       
       return members.Select(m => m.Identity).ToHashSet(); 
    }
    
    private IEnumerable<Identity> GetDeletedMembersIdentities(ReadOnlyCollection<Identity> cloudIdentities, HashSet<Identity> refIdentitiesMap)
    {
        foreach (var cloudIdentity in cloudIdentities)
        {
            if (!refIdentitiesMap.Contains(cloudIdentity))
            {
                yield return cloudIdentity;
            }
        }
    } 
    
    private async Task HandleDeletedMembers(ReadOnlyCollection<Identity> toDelete, CancellationToken cancellationToken = default)
    {
        if (toDelete.Count == 0)
        {
            _logger.LogDebug("Deleted members was not found");
            return;
        }

        _logger.LogDebug("Found deleted users: {Deleted}", toDelete.Count);
        await _userDeleter.DeleteManyAsync(toDelete, cancellationToken);
        _logger.LogDebug("Deleted members are synchronized");
        
    }
}
