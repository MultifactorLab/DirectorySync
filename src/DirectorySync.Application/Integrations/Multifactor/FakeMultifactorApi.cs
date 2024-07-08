using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor;

internal class FakeMultifactorApi : IMultifactorApi
{
    public Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken token = default)
    {
        var users = new CreateUsersOperationResult();
        foreach (var user in bucket.NewUsers)
        {
            users.AddUser(new CreatedUser(user.Identity, new MultifactorUserId(Guid.NewGuid().ToString())));
        }
        return Task.FromResult<ICreateUsersOperationResult>(users);
    }

    public Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default)
    {
        var users = new UpdateUsersOperationResult();
        foreach (var user in bucket.ModifiedUsers)
        {
            users.AddUserId(user.Id);
        }
        return Task.FromResult<IUpdateUsersOperationResult>(users);    
    }

    public Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken token = default)
    {
        var users = new DeleteUsersOperationResult();
        foreach (var user in bucket.DeletedUsers)
        {
            users.AddUserId(user);
        }
        return Task.FromResult<IDeleteUsersOperationResult>(users);        
    }
}
