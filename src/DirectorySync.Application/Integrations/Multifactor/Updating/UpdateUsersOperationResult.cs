using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IUpdateUsersOperationResult
{
    ReadOnlyCollection<HandledUser> UpdatedUsers { get; }
}

public class UpdateUsersOperationResult : IUpdateUsersOperationResult
{
    private readonly HashSet<HandledUser> _users = new();
    public ReadOnlyCollection<HandledUser> UpdatedUsers => new (_users.ToArray());

    public UpdateUsersOperationResult Add(HandledUser user)
    {
        _users.Add(user);
        return this;
    }

    public UpdateUsersOperationResult Add(IEnumerable<HandledUser> user)
    {
        foreach (var identity in user)
        {
            _users.Add(identity);
        }

        return this;
    }
}
