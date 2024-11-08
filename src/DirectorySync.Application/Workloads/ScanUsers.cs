using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Ports;
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
            _storage.InsertGroup(cachedGroup);
        }
        
        _logger.LogDebug("Searching for a new users...");
        var newDirectoryUsers = FindNewUsers(referenceGroup, cachedGroup).ToArray();
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

        _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
    }

    private async Task CreateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryUser[] referenceUsers,
        CancellationToken token)
    {
        var bucket = BuildBucket(referenceUsers);

        var createApiTimer = _timer.Start("Api Request: Create Users");
        var res = await _api.CreateManyAsync(bucket, token);
        createApiTimer.Stop();

        foreach (var created in res.CreatedUsers)
        {
            var refUser = referenceUsers.FirstOrDefault(x => x.Guid == created.Id);
            if (refUser is null)
            {
                continue;
            }

            var cachedMember = CachedDirectoryGroupMember.Create(created.Id, created.Identity, refUser.Attributes);
            group.AddMembers(cachedMember);
        }
    }

    private static IEnumerable<ReferenceDirectoryUser> FindNewUsers(ReferenceDirectoryGroup refGroup, CachedDirectoryGroup cachedGroup)
    {
        return refGroup.Members.Where(x => !cachedGroup.Members.Select(s => s.Id).Contains(x.Guid));
    }

    private NewUsersBucket BuildBucket(ReferenceDirectoryUser[] referenceUsers)
    {
        var bucket = new NewUsersBucket();
        foreach (var refUser in referenceUsers)
        {
            using var withUser = _logger.EnrichWithLdapUser(refUser.Guid);

            var props = _propertyMapper.Map(refUser.Attributes);
            if (props.Count == 0)
            {
                continue;
            }

            var identity = props[MultifactorPropertyName.IdentityProperty]!;
            var user = bucket.AddNewUser(refUser.Guid, identity);
            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }
        }

        return bucket;
    }
}
