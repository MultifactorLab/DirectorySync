using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal sealed class Updater
{
    private readonly IMultifactorApi _api;
    private readonly IApplicationStorage _storage;
    private readonly CodeTimer _codeTimer;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _attrMappingOptions;
    private readonly IOptionsMonitor<GroupMappingsOptions> _groupsMappingOptions;
    private readonly UserProcessingOptions _options;

    public Updater(IMultifactorApi api,
        IApplicationStorage storage,
        CodeTimer codeTimer,
        IOptions<UserProcessingOptions> options,
        IOptionsMonitor<LdapAttributeMappingOptions> attrMappingOptions,
        IOptionsMonitor<GroupMappingsOptions> groupsMappingOptions)
    {
        _api = api;
        _storage = storage;
        _codeTimer = codeTimer;
        _attrMappingOptions = attrMappingOptions;
        _options = options.Value;
        _groupsMappingOptions = groupsMappingOptions;
    }

    public async Task UpdateManyAsync(CachedDirectoryGroup group,
        ReferenceDirectoryUser[] modified,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(modified);

        if (modified.Length == 0)
        {
            return;
        }

        var options = _attrMappingOptions.CurrentValue;

        var skip = 0;
        while (true)
        {
            var bucket = new ModifiedUsersBucket();
            foreach (var member in modified.Skip(skip).Take(_options.UpdatingBatchSize))
            {
                var cachedMember = group.Members.First(x => x.Id == member.Guid);

                var signUpGroupsToRemove = new List<string>();
                foreach (var groupGuid in member.UnlinkedGroups)
                {
                    if (_groupsMappingOptions.CurrentValue.DirectoryGroupMappings
                        .TryGetValue(group.GroupGuid.Value.ToString(), out var groupsToRemove))
                    {
                        signUpGroupsToRemove.AddRange(groupsToRemove);
                    }

                }

                var groupsChanges = new SignUpGroupChanges()
                {
                    SignUpGroupsToRemove = signUpGroupsToRemove.ToArray() ?? Array.Empty<string>()
                };

                var user = bucket.Add(cachedMember.Id, cachedMember.Identity, groupsChanges);

                SetProperties(options, member, user);
            }

            if (bucket.Count == 0)
            {
                break;
            }

            var updateApiTimer = _codeTimer.Start("Api Request: Update Users");
            var res = await _api.UpdateManyAsync(bucket, ct);
            updateApiTimer.Stop();

            UpdateCachedGroup(group, modified, res);
            skip += bucket.Count;
        }
    }

    private static void SetProperties(LdapAttributeMappingOptions options, 
        ReferenceDirectoryUser refUser, 
        ModifiedUser user)
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
        ReferenceDirectoryUser[] modified, 
        IUpdateUsersOperationResult res)
    {
        foreach (var user in res.UpdatedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.Id == user.Id);
            if (cachedUser is null)
            {
                continue;
            }

            var refMember = modified.First(x => x.Guid == cachedUser.Id);

            if (refMember.UnlinkedGroups.Any())
            {
                group.DeleteMembers(cachedUser.Id);
            }
            else
            {
                var hash = new AttributesHash(refMember.Attributes);
                cachedUser.UpdateHash(hash);
            }
        }

        _storage.UpdateGroup(group);
    }
}
