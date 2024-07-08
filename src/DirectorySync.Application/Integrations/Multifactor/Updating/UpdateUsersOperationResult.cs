using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

internal class UpdateUsersOperationResult : IUpdateUsersOperationResult
{
    private readonly HashSet<MultifactorUserId> _updatedUsers = new();
    public ReadOnlyCollection<MultifactorUserId> UpdatedUsers => new (_updatedUsers.ToArray());

    public UpdateUsersOperationResult AddUserId(MultifactorUserId userId)
    {
        _updatedUsers.Add(userId);
        return this;
    }
}
