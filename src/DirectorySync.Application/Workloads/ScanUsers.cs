using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Entities;
using DirectorySync.Application.Ports;
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
    private readonly Creator _creator;
    private readonly CodeTimer _timer;
    private readonly ILogger<ScanUsers> _logger;

    public ScanUsers(RequiredLdapAttributes requiredLdapAttributes,
        IGetReferenceGroup getReferenceGroup,
        IApplicationStorage storage,
        Creator creator,
        CodeTimer timer,
        ILogger<ScanUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroup = getReferenceGroup;
        _storage = storage;
        _creator = creator;
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
        await _creator.CreateManyAsync(cachedGroup, newDirectoryUsers, token);

        var cacheGroupTimer = _timer.Start("Cache Group");
        _storage.UpdateGroup(cachedGroup);
        cacheGroupTimer.Stop();

        _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
    }

    private static IEnumerable<ReferenceDirectoryUser> FindNewUsers(ReferenceDirectoryGroup refGroup, CachedDirectoryGroup cachedGroup)
    {
        return refGroup.Members.Where(x => !cachedGroup.Members.Select(s => s.Id).Contains(x.Guid));
    }
}
