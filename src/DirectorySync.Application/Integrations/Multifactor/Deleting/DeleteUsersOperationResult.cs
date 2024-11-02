using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeleteUsersOperationResult
{
    ReadOnlyCollection<HandledUser> DeletedUsers { get; }
}

public class DeleteUsersOperationResult : IDeleteUsersOperationResult
{
    private readonly HashSet<HandledUser> _users = new();
    public ReadOnlyCollection<HandledUser> DeletedUsers => new (_users.ToArray());

    public DeleteUsersOperationResult Add(HandledUser user)
    {
        _users.Add(user);
        return this;
    }

    public DeleteUsersOperationResult Add(IEnumerable<HandledUser> users)
    {
        foreach (var user in users)
        {
            _users.Add(user);
        }

        return this;
    }
}
