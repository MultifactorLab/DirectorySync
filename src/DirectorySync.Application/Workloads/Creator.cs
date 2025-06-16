using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Entities;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal sealed class Creator
{
    private readonly IMultifactorApi _api;
    private readonly IApplicationStorage _storage;
    private readonly CodeTimer _codeTimer;
    private readonly UserProcessingOptions _options;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _attrMappingOptions;
    private readonly IOptionsMonitor<GroupMappingsOptions> _groupMappingOptions;

    public Creator(IMultifactorApi api,
        IApplicationStorage storage,
        CodeTimer codeTimer,
        IOptions<UserProcessingOptions> options,
        IOptionsMonitor<LdapAttributeMappingOptions> attrMappingOptions,
        IOptionsMonitor<GroupMappingsOptions> groupMappingOptions)
    {
        _api = api;
        _storage = storage;
        _codeTimer = codeTimer;
        _options = options.Value;
        _attrMappingOptions = attrMappingOptions;
        _groupMappingOptions = groupMappingOptions;
    }

    public async Task CreateManyAsync(CachedDirectoryGroup group,
        ReferenceDirectoryUser[] created,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(created);

        if (created.Length == 0)
        {
            return;
        }

        var options = _attrMappingOptions.CurrentValue;

        var groupsMappingOptions = _groupMappingOptions.CurrentValue.DirectoryGroupMappings
            .ToDictionary(
                kpv => new DirectoryGuid(Guid.Parse(kpv.DirectoryGroup)),
                kpv => kpv.SignUpGroups.ToArray()
            );

        groupsMappingOptions.TryGetValue(group.GroupGuid, out var groupsToAdd);

        var groupsChanges = new SignUpGroupChanges()
        {
            SignUpGroupsToAdd = groupsToAdd ?? Array.Empty<string>()
        };

        var skip = 0;
        while (true)
        {
            var bucket = new NewUsersBucket();
            foreach (var refUser in created.Skip(skip).Take(_options.CreatingBatchSize))
            {
                var identity = refUser.Attributes.GetSingleOrDefault(options.IdentityAttribute);
                if (identity is null)
                {
                    continue;
                }                

                var user = bucket.AddNewUser(refUser.Guid, identity, groupsChanges);

                SetProperties(options, refUser, user);
            }

            if (bucket.Count == 0)
            {
                break;
            }

            var updateApiTimer = _codeTimer.Start("Api Request: Create Users");
            var res = await _api.CreateManyAsync(bucket, ct);
            updateApiTimer.Stop();

            UpdateCachedGroup(group, created, res);
            skip += bucket.Count;
        }
    }

    private static void SetProperties(LdapAttributeMappingOptions options, 
        ReferenceDirectoryUser refUser, 
        NewUser user)
    {
        if (!string.IsNullOrWhiteSpace(options.NameAttribute))
        {
            var name = refUser.Attributes.GetFirstOrDefault(options.NameAttribute);
            if (name is not null)
            {
                user.AddProperty(MultifactorPropertyName.AdditionalProperties.NameProperty, name);
            }
        }

        var email = refUser.Attributes.GetFirstOrDefault(options.EmailAttributes);
        if (email is not null)
        {
            user.AddProperty(MultifactorPropertyName.AdditionalProperties.EmailProperty, email);
        }

        var phone = refUser.Attributes.GetFirstOrDefault(options.PhoneAttributes);
        if (phone is not null)
        {
            user.AddProperty(MultifactorPropertyName.AdditionalProperties.PhoneProperty, phone);
        }
    }

    private void UpdateCachedGroup(CachedDirectoryGroup group,
        ReferenceDirectoryUser[] referenceUsers,
        ICreateUsersOperationResult res)
    {
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

        _storage.UpdateGroup(group);
    }
}
