using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

internal class MultifactorApi : IMultifactorApi
{
    public Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
