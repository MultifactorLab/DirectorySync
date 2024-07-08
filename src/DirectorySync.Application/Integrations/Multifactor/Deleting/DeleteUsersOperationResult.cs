using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

internal class DeleteUsersOperationResult : IDeleteUsersOperationResult
{
    private readonly HashSet<MultifactorUserId> _deletedUsers = new();
    public ReadOnlyCollection<MultifactorUserId> DeletedUsers => new (_deletedUsers.ToArray());

    public DeleteUsersOperationResult AddUserId(MultifactorUserId userId)
    {
        _deletedUsers.Add(userId);
        return this;
    }
}
