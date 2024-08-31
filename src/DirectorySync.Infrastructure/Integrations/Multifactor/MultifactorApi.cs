using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

internal class MultifactorApi : IMultifactorApi
{
    public Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        throw new NotImplementedException();
    }

    public Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        throw new NotImplementedException();
    }

    public Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        throw new NotImplementedException();
    }
}
