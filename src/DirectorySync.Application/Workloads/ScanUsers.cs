using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Measuring;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

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
        _logger.LogDebug("Required attributes: {Attrs:l}", string.Join(",", names));
        
        var getGroupTimer = _timer.Start("Get Reference Group");
        var referenceGroup = _getReferenceGroup.Execute(groupGuid, names);
        getGroupTimer.Stop();
        _logger.LogDebug("Reference group found: {Group:l}", referenceGroup);
        
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        ReferenceDirectoryGroupMember[] createdUsers;
        
        if (cachedGroup is null)
        {
            _logger.LogDebug("Reference group is not cached and now it will");
            cachedGroup = CachedDirectoryGroup.Create(referenceGroup.Guid, []);
            _storage.CreateGroup(cachedGroup);

            createdUsers = referenceGroup.Members.ToArray();
        }
        else
        {
            _logger.LogDebug("Searching for new users...");
            createdUsers = referenceGroup.Members
                .Where(x => !cachedGroup.Members.Select(s => s.Guid).Contains(x.Guid))
                .ToArray();
        }
        
        if (createdUsers.Length == 0)
        {
            _logger.LogDebug("New users was not found");
            _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
            return;
        }

        _logger.LogDebug("Found new users: {New}", createdUsers.Length);
        
        await ProcessInPortionsAsync(createdUsers, cachedGroup, token);
        
        _logger.LogInformation(ApplicationEvent.CompleteUserScanning, "Complete users scanning for group {group}", groupGuid);
    }
    
    private async Task ProcessInPortionsAsync(ReferenceDirectoryGroupMember[] members, 
        CachedDirectoryGroup group, 
        CancellationToken token)
    {
        var bucket = new NewUsersBucket();
        var identityWithGuids = new Dictionary<string, DirectoryGuid>();

        var skip = 0;
        const int take = 50;
        
        do
        {
            var portion = members.Skip(skip).Take(take).ToArray();

            PrepareCollections(portion, bucket, identityWithGuids);
            
            var createApiTimer = _timer.Start("Api Request: Create Users");
            var res = await _api.CreateManyAsync(bucket, token);
            createApiTimer.Stop();
        
            // mutate cached group
            foreach (var user in res.CreatedUsers)
            {
                if (!identityWithGuids.TryGetValue(user.Identity, out var guid))
                {
                    continue;
                }

                var member = members.First(x => x.Guid == guid);
                var cachedMember = CachedDirectoryGroupMember.Create(member.Guid, user.Id, member.Attributes);
                group.AddMembers(cachedMember);
            }
            
            // update cached group
            var cacheGroupTimer = _timer.Start("Cache Created Users");
            _storage.UpdateGroup(group);
            cacheGroupTimer.Stop();
            
            skip += portion.Length;
            _logger.LogDebug("Users was created and cached ({Processed} of {Total})", skip, members.Length);
            
            bucket.Clear();
            identityWithGuids.Clear();
        } while (skip < members.Length);
    }

    private void PrepareCollections(ReferenceDirectoryGroupMember[] portion, 
        NewUsersBucket bucket,
        Dictionary<string, DirectoryGuid> identityWithGuids)
    {
        foreach (var member in portion)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);
            
            var props = _propertyMapper.Map(member.Attributes);
            var user = bucket.AddNewUser(props[MultifactorPropertyName.IdentityProperty]!);
            
            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }
            
            identityWithGuids[user.Identity] = member.Guid;
        }
    }
}
