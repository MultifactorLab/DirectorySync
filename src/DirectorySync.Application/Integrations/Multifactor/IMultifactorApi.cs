using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Get;
using DirectorySync.Application.Integrations.Multifactor.Updating;

namespace DirectorySync.Application.Integrations.Multifactor;

public interface IMultifactorApi
{
    Task<IGetUsersIdentitiesOperationResult> GetUsersIdentitesAsync(CancellationToken ct = default);
    Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken ct = default);
    Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken ct = default);
    Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken ct = default);
}
