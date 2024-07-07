using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Ldap.Extensions;
using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Workloads;

public class HandleNewUsers
{
    private readonly RequiredLdapAttributes _requiredLdapAttributes;
    private readonly GetReferenceGroupByGuid _getReferenceGroupByGuid;
    private readonly IApplicationStorage _storage;
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly ILogger<HandleNewUsers> _logger;

    internal HandleNewUsers(RequiredLdapAttributes requiredLdapAttributes,
        GetReferenceGroupByGuid getReferenceGroupByGuid,
        IApplicationStorage storage,
        MultifactorPropertyMapper propertyMapper,
        IMultifactorApi api,
        ILogger<HandleNewUsers> logger)
    {
        _requiredLdapAttributes = requiredLdapAttributes;
        _getReferenceGroupByGuid = getReferenceGroupByGuid;
        _storage = storage;
        _propertyMapper = propertyMapper;
        _api = api;
        _logger = logger;
    }

    public Task ExecuteAsync(Guid groupGuid, CancellationToken token = default)
    {
        using var withGroup = _logger.EnrichWithGroup(groupGuid);
        _logger.LogDebug("New users handling started");
        
        var names = _requiredLdapAttributes.GetNames().ToArray();
        var referenceGroup = _getReferenceGroupByGuid.Execute(groupGuid, names);
        
        var cachedGroup = _storage.FindGroup(referenceGroup.Guid);
        if (cachedGroup is null)
        {
            _logger.LogDebug("Reference group is not cached and now it will");
            cachedGroup = referenceGroup.ToCachedDirectoryGroup();
        }

        var withNoId = cachedGroup.Members
            .Where(x => x.UserId == MultifactorUserId.Undefined)
            .Select(x => x.Guid);
        var created = referenceGroup.Members.Where(x => withNoId.Contains(x.Guid)).ToArray();
        if (created.Length == 0)
        {
            _logger.LogDebug("Nothing to create");
            return Task.CompletedTask;
        }

        return CreateAsync(cachedGroup, created, token);
    }

    private async Task CreateAsync(CachedDirectoryGroup group, 
        ReferenceDirectoryGroupMember[] created,
        CancellationToken token)
    {
        var bucket = new NewUsersBucket();
        foreach (var member in created)
        {
            using var withUser = _logger.EnrichWithLdapUser(member.Guid);
            
            var props = _propertyMapper.Map(member.Attributes);
            var user = bucket.AddNewUser(props[MultifactorPropertyName.IdentityProperty]!);
            
            foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
            {
                user.AddProperty(prop.Key, prop.Value);
            }
        }
        
        var res = await _api.CreateManyAsync(bucket, token);
        foreach (var user in res.CreatedUsers)
        {
            var props = _propertyMapper.Map(member.Attributes);
            var guid = created.FirstOrDefault(x => x.)
        }
    }
}
