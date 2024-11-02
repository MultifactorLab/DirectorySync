using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Ports;
using DirectorySync.Domain;
using DirectorySync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Workloads;

internal class Deleter
{
    private readonly IMultifactorApi _api;
    private readonly IApplicationStorage _storage;
    private readonly CodeTimer _codeTimer;
    private readonly UserProcessingOptions _options;
    private readonly ILogger<Deleter> _logger;

    public Deleter(IMultifactorApi api,
        IApplicationStorage storage,
        CodeTimer codeTimer,
        IOptions<UserProcessingOptions> options,
        ILogger<Deleter> logger)
    {
        _api = api;
        _storage = storage;
        _codeTimer = codeTimer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DeleteManyAsync(CachedDirectoryGroup group,
        DirectoryGuid[] deletedUsers,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(deletedUsers);

        var batch = group.Members
            .IntersectBy(deletedUsers, x => x.Id)
            .ToArray();

        if (batch.Length == 0)
        {
            return;
        }

        var skip = 0;
        while (true)
        {
            var bucket = new DeletedUsersBucket();
            foreach (var userToDelete in batch.Skip(skip).Take(_options.DeletingBatchSize))
            {
                bucket.Add(userToDelete.Id, userToDelete.Identity);
            }

            if (bucket.Count == 0)
            {
                break;
            }

            _logger.LogDebug("Trying to delete users: {Portion}", bucket.Count);

            var deleteApiTimer = _codeTimer.Start("Api Request: Delete Users");
            var res = await _api.DeleteManyAsync(bucket, ct);
            deleteApiTimer.Stop();

            UpdateCachedGroup(group, res);
            skip += bucket.Count;
        }
    }

    private void UpdateCachedGroup(CachedDirectoryGroup group, IDeleteUsersOperationResult res)
    {
        foreach (var user in res.DeletedUsers)
        {
            var cachedUser = group.Members.FirstOrDefault(x => x.Id == user.Id);
            if (cachedUser is not null)
            {
                group.DeleteMembers(cachedUser.Id);
            }
        }

        _storage.UpdateGroup(group);
    }
}
