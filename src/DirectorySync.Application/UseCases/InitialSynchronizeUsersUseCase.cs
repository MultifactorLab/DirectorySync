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
    Task ExecuteAsync(DirectoryGuid[] trackingGroupGuids, CancellationToken cancellationToken = default);
}

public class InitialSynchronizeUsersUseCase : IInitialSynchronizeUsersUseCase
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

    public async Task ExecuteAsync(DirectoryGuid[] trackingGroupGuids, CancellationToken cancellationToken = default)
    {
       if (trackingGroupGuids.Length == 0)
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

       var cloudUsers = await _userCloudPort.GetUsersAsync(cancellationToken);
       _logger.LogDebug("Fetched {Count} users from cloud", cloudUsers.Count);

       var requiredAttributes = _syncSettingsOptions.GetRequiredAttributeNames();
       _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", requiredAttributes));

       var adMembers = GetTrackingReferenceMembers(trackingGroupGuids, requiredAttributes);

       var toDelete = GetDeletedCloudUsers(cloudUsers, adMembers)
           .ToList()
           .AsReadOnly();
       
       _logger.LogInformation("Identified {Count} deleted members to handle", toDelete.Count);
       
       await HandleDeletedMembers(toDelete, cancellationToken);
    } 

    private ReadOnlyCollection<MemberModel> GetTrackingReferenceMembers(IEnumerable<DirectoryGuid> trackingGroups,
        string[] requiredAttributes)
    { 
        var (referenceGroups, searchDomains) = _ldapGroupPort.GetByGuid(trackingGroups); 
        if (referenceGroups.Count == 0)
        {
           _logger.LogWarning("No reference groups found for given tracking groups");
           return ReadOnlyCollection<MemberModel>.Empty;
        }

        var members = new List<MemberModel>();

        foreach (var referenceGroup in referenceGroups)
        {
           members.AddRange(_ldapMemberPort.GetByGuids(referenceGroup.MemberIds, requiredAttributes, searchDomains.ToArray()));
        }

        return members.AsReadOnly();
    }
    
    private IEnumerable<Identity> GetDeletedCloudUsers(
        ReadOnlyCollection<CloudUserModel> cloudUsers,
        ReadOnlyCollection<MemberModel> adMembers)
    {
        var adGuidSet = adMembers
            .Select(m => m.Id.Value)
            .ToHashSet();
        
        var adIdentitySet = adMembers
            .Select(m => m.Identity)
            .ToHashSet();

        foreach (var cloudUser in cloudUsers)
        {
            var presentByGuid = cloudUser.ExternalObjectId is not null
                && adGuidSet.Contains(cloudUser.ExternalObjectId.Value);

            if (presentByGuid)
            {
                continue;
            }

            if (!adIdentitySet.Contains(cloudUser.Identity))
            {
                yield return cloudUser.Identity;
            }
        }
    }
    
    private async Task HandleDeletedMembers(ReadOnlyCollection<Identity> toDeleteIdentities,
        CancellationToken cancellationToken = default)
    {
        if (toDeleteIdentities.Count == 0)
        {
            _logger.LogDebug("Deleted members was not found");
            return;
        }
        
        var toDelete = toDeleteIdentities
            .Select(u => MemberModel.Create(Guid.NewGuid(), u, []))
            .ToList();

        _logger.LogDebug("Found deleted users: {Deleted}", toDeleteIdentities.Count);
        await _userDeleter.DeleteManyAsync(toDelete, cancellationToken);
        _logger.LogDebug("Deleted members are synchronized");
    }
}
