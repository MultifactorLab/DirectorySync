using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Models;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

public interface ISynchronizeCloud
{
    Task ExecuteAsync(Guid[] trackingGroups, CancellationToken token = default);
}

internal class SynchronizeCloud : ISynchronizeCloud
{
    private readonly IApplicationStorage _storage;
    private readonly IMultifactorApi _multifactorApi;
    private readonly IGetReferenceGroup _getReferenceGroup;
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _attrMappingOptions;
    private readonly Deleter _deleter;
    private readonly ILogger<SynchronizeCloud> _logger;

    public SynchronizeCloud(IApplicationStorage storage,
        IMultifactorApi multifactorApi,
        IGetReferenceGroup getReferenceGroup,
        RequiredLdapAttributes requiredLdapAttributes,
        IOptionsMonitor<LdapAttributeMappingOptions> attrMappingOptions,
        Deleter deleter,
        ILogger<SynchronizeCloud> logger)
    {
        _storage = storage;
        _multifactorApi = multifactorApi;
        _getReferenceGroup = getReferenceGroup;
        _requiredLdapAttributes = requiredLdapAttributes;
        _attrMappingOptions = attrMappingOptions;
        _deleter = deleter;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid[]? trackingGroups, CancellationToken cancellationToken = default)
    {
        if (trackingGroups is null || trackingGroups.Length == 0)
        {
            _logger.LogDebug("No tracking groups provided, skipping synchronization");
            return;
        }

        _logger.LogDebug("Tracking group GUIDs: {GroupGUIDs}", string.Join(", ", trackingGroups));

        if (_storage.IsGroupCollectionExists())
        {
            _logger.LogDebug("Local storage already exists, skipping synchronization");
            return;
        }

        var cloudIdentities = await _multifactorApi.GetUsersIdentitesAsync();
        _logger.LogDebug("Fetched {Count} identities from cloud with user name format {UserNameFormat}", 
            cloudIdentities.Identities.Count,
            cloudIdentities.UserNameFormat);

        var requiredAttributes = _requiredLdapAttributes.GetNames();
        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", requiredAttributes));

        var referenceGroups = GetTrackingReferenceGroups(trackingGroups, requiredAttributes).ToArray();
        _logger.LogDebug("Retrieved {Count} reference groups for tracking", referenceGroups.Length);

        if (referenceGroups.Length == 0)
        {
            _logger.LogWarning("No reference groups found for given tracking groups");
            return;
        }

        var attrOptions = _attrMappingOptions.CurrentValue;
        var memberGroupMap = ReferenceMembershipModel.BuildMemberGroupMap(referenceGroups, attrOptions, cloudIdentities.UserNameFormat);

        var deletedMembers = GetDeletedMembersIdentities(cloudIdentities.Identities, memberGroupMap).ToArray();
        _logger.LogInformation("Identified {Count} deleted members to handle", deletedMembers.Length);

        await HandleDeletedMembers(deletedMembers, cancellationToken);
    }
    private IReadOnlyCollection<ReferenceDirectoryGroup> GetTrackingReferenceGroups(Guid[] trackingGroups, string[] requiredAttributes)
    {
        var bag = new ConcurrentBag<ReferenceDirectoryGroup>();

        Parallel.ForEach(trackingGroups, trackingGroup =>
        {
            var referenceGroup = _getReferenceGroup.Execute(new DirectoryGuid(trackingGroup), requiredAttributes);
            if (referenceGroup is null)
            {
                throw new InvalidOperationException($"Reference group not found for tracking group {trackingGroup}");
            }

            bag.Add(referenceGroup);
        });

        return bag;
    }

    private IEnumerable<string> GetDeletedMembersIdentities(ReadOnlyCollection<MultifactorIdentity> cloudIdentities, ReferenceMembershipModel membership)
    {
        foreach (var cloudIdentity in cloudIdentities)
        {
            if (!membership.MemborshipMap.ContainsKey(cloudIdentity))
            {
                yield return cloudIdentity;
            }
        }
    }

    private async Task HandleDeletedMembers(string[] deletedIdentites, CancellationToken cancellationToken = default)
    {
        if (deletedIdentites.Length == 0)
        {
            _logger.LogDebug("Deleted members was not found");
            return;
        }

        _logger.LogDebug("Found deleted users: {Deleted}", deletedIdentites.Length);
        await _deleter.DeleteManyAsync(deletedIdentites, cancellationToken);
        _logger.LogDebug("Deleted members are synchronized");
    }
}
