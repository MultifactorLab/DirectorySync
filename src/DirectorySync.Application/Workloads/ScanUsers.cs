using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Measuring;
using DirectorySync.Domain;
using DirectorySync.Domain.Abstractions;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

/// <summary>
/// Scans for a new users and pushes its to a Multifactor Cloud.
/// </summary>
public interface IScanUsers
{
    Task ExecuteAsync(Guid groupGuid, CancellationToken token = default);
}

internal class ScanUsers : IScanUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly IGetReferenceGroup _getReferenceGroup;
    private readonly IApplicationStorage _storage;
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly CodeTimer _timer;
    private readonly ILogger<ScanUsers> _logger;

    public ScanUsers(RequiredLdapAttributes requiredLdapAttributes,
        IGetReferenceGroup getReferenceGroup,
        IApplicationStorage storage,
        MultifactorPropertyMapper propertyMapper,
        IMultifactorApi api,
        CodeTimer timer,
        ILogger<ScanUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroup = getReferenceGroup;
        _storage = storage;
        _propertyMapper = propertyMapper;
        _api = api;
        _timer = timer;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogInformation(ApplicationEvent.StartUserScanning, "Start users scanning for group {group}", groupGuid);
        
        var names = _requiredLdapAttributes.GetNames().ToArray();
        if (names.Length == 0)
        {
            _logger.LogWarning(ApplicationEvent.InvalidServiceConfiguration, "Required LDAP attributes not defined. Please check attribute mapping");
            return;
        }

        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", names));
        
        var getGroupTimer = _timer.Start("Get Reference Group");
        var referenceGroup = _getReferenceGroup.Execute(groupGuid, names);
        getGroupTimer.Stop();
        _logger.LogDebug("Reference group found: {Group:l}", referenceGroup);
        
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug("Reference group is not cached and now it will");
            cachedGroup = CachedDirectoryGroup.Create(referenceGroup.Guid, []);
            _storage.CreateGroup(cachedGroup);
        }

        var nonPropagated = cachedGroup.Members
            .Where(x => !x.Propagated)
            .Select(x => x.Guid);
        
        _logger.LogDebug("Searching for a new users...");
        var newDirectoryUsers = referenceGroup.Members.Where(x => nonPropagated.Contains(x.Guid)).ToArray();
        if (newDirectoryUsers.Length == 0)
        {
            _logger.LogDebug("New users was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
            return;
        }

        _logger.LogDebug("Found new users: {New}", newDirectoryUsers.Length);
        
        await CreateAsync(cachedGroup, newDirectoryUsers, token);
        
        var cacheGroupTimer = _timer.Start("Cache Group");
        _storage.UpdateGroup(cachedGroup);
        cacheGroupTimer.Stop();

        _logger.LogDebug("New users was created on the MF server side and group was cached");
        _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
    }

    private async Task CreateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryUser[] created,
        CancellationToken token)
    {
        var identityWithGuids = new Dictionary<string, DirectoryGuid>();
        var bucket = BuildBucket(created, identityWithGuids);

        var createApiTimer = _timer.Start("Api Request: Create Users");
        var res = await _api.CreateManyAsync(bucket, token);
        createApiTimer.Stop();

        foreach (var user in res.CreatedUsers)
        {
            if (!identityWithGuids.TryGetValue(user.Identity, out var guid))
            {
                continue;
            }

            var cached = group.Members.First(x => x.Guid == guid);
            cached.Propagate();
        }
    }

    private NewUsersBucket BuildBucket(ReferenceDirectoryUser[] created, Dictionary<string, DirectoryGuid> identityWithGuids)
    {
        var bucket = new NewUsersBucket();
        foreach (var member in created)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);

            var props = _propertyMapper.Map(member.Attributes);
            if (props.Count == 0)
            {
                continue;
            }

            var identity = props[MultifactorPropertyName.IdentityProperty]!;
            var user = bucket.AddNewUser(identity);
            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }

            identityWithGuids[identity] = member.Guid;
        }

        return bucket;
    }
}
