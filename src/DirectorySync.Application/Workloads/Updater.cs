using DirectorySync.Application.Extensions;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Application.Measuring;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal class Updater
{
    private readonly MultifactorPropertyMapper _propertyMapper;
    private readonly IMultifactorApi _api;
    private readonly IApplicationStorage _storage;
    private readonly CodeTimer _codeTimer;
    private readonly UserProcessingOptions _options;
    private readonly ILogger<Updater> _logger;

    public Updater(MultifactorPropertyMapper propertyMapper, 
        IMultifactorApi api,
        IApplicationStorage storage,
        CodeTimer codeTimer,
        IOptions<UserProcessingOptions> options,
        ILogger<Updater> logger)
    {
        _propertyMapper = propertyMapper;
        _api = api;
        _storage = storage;
        _codeTimer = codeTimer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UpdateManyAsync(CachedDirectoryGroup group,
        ReferenceDirectoryUser[] modified,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(modified);

        if (modified.Length == 0)
        {
            return;
        }

        var skip = 0;
        while (true)
        {
            var bucket = new ModifiedUsersBucket();
            foreach (var member in modified.Skip(skip).Take(_options.UpdatingBatchSize))
            {
                using var logWithUser = _logger.EnrichWithLdapUser(member.Guid);

                var props = _propertyMapper.Map(member.Attributes.ToArray());

                var cachedMember = group.Members.First(x => x.Guid == member.Guid);
                var user = bucket.Add(cachedMember.Identity);

                foreach (var prop in props.Where(x => !x.Key.Equals(MultifactorPropertyName.IdentityProperty, StringComparison.OrdinalIgnoreCase)))
                {
                    user.AddProperty(prop.Key, prop.Value);
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

    private void UpdateCachedGroup(CachedDirectoryGroup group, ReferenceDirectoryUser[] modified, IUpdateUsersOperationResult res)
    {
        foreach (var id in res.UpdatedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.Identity == id);
            if (cachedUser is null)
            {
                continue;
            }

            var refMember = modified.First(x => x.Guid == cachedUser.Guid);
            var hash = new AttributesHash(refMember.Attributes);
            cachedUser.UpdateHash(hash);
        }

        _storage.UpdateGroup(group);
    }
}
