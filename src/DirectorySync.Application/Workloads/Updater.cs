using System.Collections.ObjectModel;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models;
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
    private readonly UserProcessingOptions _options;
    private readonly IOptionsMonitor<LdapAttributeMappingOptions> _attrMappingOptions;
    private readonly IOptionsMonitor<GroupMappingsOptions> _groupMappingOptions;

    public Updater(IMultifactorApi api,
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

    public async Task UpdateManyAsync(CachedDirectoryGroup group,
        ReferenceDirectoryUserUpdateModel[] modified,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(modified);

        if (modified.Length == 0)
        {
            return;
        }

        var options = _attrMappingOptions.CurrentValue;

        var groupsMappingOptions = _groupMappingOptions.CurrentValue.DirectoryGroupMappings
            .ToDictionary(
                kpv => new DirectoryGuid(Guid.Parse(kpv.DirectoryGroup)),
                kpv => kpv.SignUpGroups.ToArray()
            );

        groupsMappingOptions.TryGetValue(group.GroupGuid, out var groupsToRemove);

        var skip = 0;
        while (true)
        {
            var bucket = new ModifiedUsersBucket();
            foreach (var member in modified.Skip(skip).Take(_options.UpdatingBatchSize))
            {
                var cachedMember = group.Members.FirstOrDefault(x => x.Id == member.Guid);

                if (cachedMember != null)
                {
                    var groupsChanges = member.IsUnlinkedFromGroup ?
                    GetUserSignUpGroupChanges(member.UserGroupIds, groupsToRemove, groupsMappingOptions)
                    : new SignUpGroupChanges();

                    var user = bucket.Add(cachedMember.Id, cachedMember.Identity, groupsChanges);

                    SetProperties(options, member, user);
                }
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

    private SignUpGroupChanges GetUserSignUpGroupChanges(ReadOnlyCollection<DirectoryGuid> userGroups, 
        string[] groupsToRemove,
        Dictionary<DirectoryGuid, string[]> groupsMapping)
    {
        if (groupsToRemove.Length == 0 || userGroups.Count == 0)
        {
            return new SignUpGroupChanges();
        }
        
        var userSignUpGroups = new List<string>();

        foreach (var group in userGroups)
        {
            if (groupsMapping.TryGetValue(group, out var signUpGroups))
            {
                userSignUpGroups.AddRange(signUpGroups);
            }
        }

        var remainingUserGroups = new List<string>(userSignUpGroups);

        foreach (var groupToRemove in groupsToRemove)
        {
            var index = remainingUserGroups.IndexOf(groupToRemove);
            if (index != -1)
            {
                remainingUserGroups.RemoveAt(index);
            }
        }

        var signUpGroupsToRemove = groupsToRemove
            .Where(group => !remainingUserGroups.Contains(group))
            .Distinct()
            .ToArray();

        return new SignUpGroupChanges
        {
            SignUpGroupsToRemove = signUpGroupsToRemove
        };
    }

    private static void SetProperties(LdapAttributeMappingOptions options,
        ReferenceDirectoryUserUpdateModel refUser, 
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
        ReferenceDirectoryUserUpdateModel[] modified, 
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

            if (refMember.IsUnlinkedFromGroup)
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
