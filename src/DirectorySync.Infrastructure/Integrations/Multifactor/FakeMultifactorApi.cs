using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Get;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

internal class FakeMultifactorApi : IMultifactorApi
{
    private readonly ILogger<FakeMultifactorApi> _logger;

    public FakeMultifactorApi(ILogger<FakeMultifactorApi> logger)
    {
        _logger = logger;
    }

    public Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        _logger.LogDebug("Sending request to API: CREATE");
        var users = new CreateUsersOperationResult();
        foreach (var user in bucket.NewUsers.Select(x => new HandledUser(x.Id, x.Identity)))
        {
            users.Add(user);
        }
        _logger.LogDebug("Got successful response from API");
        return Task.FromResult<ICreateUsersOperationResult>(users);
    }

    public Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        _logger.LogDebug("Sending request to API: UPDATE");
        var users = new UpdateUsersOperationResult();
        foreach (var user in bucket.ModifiedUsers.Select(x => new HandledUser(x.Id, x.Identity)))
        {
            users.Add(user);
        }
        _logger.LogDebug("Got successful response from API");
        return Task.FromResult<IUpdateUsersOperationResult>(users);
    }

    public Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        _logger.LogDebug("Sending request to API: DELETE");
        var users = new DeleteUsersOperationResult();
        foreach (var user in bucket.DeletedUsers.Select(x => new HandledUser(x.Id, x.Identity)))
        {
            users.Add(user);
        }
        _logger.LogDebug("Got successful response from API");
        return Task.FromResult<IDeleteUsersOperationResult>(users);
    }

    public Task<IGetUsersIdentitiesOperationResult> GetUsersIdentitesAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
