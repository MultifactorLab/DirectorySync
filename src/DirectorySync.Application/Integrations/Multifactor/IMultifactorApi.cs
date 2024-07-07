using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;

namespace DirectorySync.Application.Integrations.Multifactor;

public interface IMultifactorApi
{
    Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken token = default);
    Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default);
    Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken token = default);
}
