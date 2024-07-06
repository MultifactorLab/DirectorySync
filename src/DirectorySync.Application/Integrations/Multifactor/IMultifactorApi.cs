using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor;

public interface IMultifactorApi
{
    Task CreateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default);
    Task UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken token = default);
    Task DeleteManyAsync(IEnumerable<MultifactorUserId> identifiers, CancellationToken token = default);
}
