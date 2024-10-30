using DirectorySync.Application.Integrations.Multifactor.Updating;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeleteUsersOperationResult
{
    ReadOnlyCollection<string> DeletedUsers { get; }
}

public class DeleteUsersOperationResult : IDeleteUsersOperationResult
{
    private readonly HashSet<string> _deletedUsers = new();
    public ReadOnlyCollection<string> DeletedUsers => new (_deletedUsers.ToArray());

    public DeleteUsersOperationResult Add(string identity)
    {
        _deletedUsers.Add(identity);
        return this;
    }

    public DeleteUsersOperationResult Add(IEnumerable<string> identities)
    {
        foreach (var identity in identities)
        {
            _deletedUsers.Add(identity);
        }

        return this;
    }
}
