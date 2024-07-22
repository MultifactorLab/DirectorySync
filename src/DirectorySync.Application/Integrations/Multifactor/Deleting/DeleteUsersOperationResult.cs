using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

internal class DeleteUsersOperationResult : IDeleteUsersOperationResult
{
    private readonly HashSet<MultifactorIdentity> _deletedUsers = new();
    public ReadOnlyCollection<MultifactorIdentity> DeletedUsers => new (_deletedUsers.ToArray());

    public DeleteUsersOperationResult AddUserId(MultifactorIdentity userId)
    {
        _deletedUsers.Add(userId);
        return this;
    }
}
