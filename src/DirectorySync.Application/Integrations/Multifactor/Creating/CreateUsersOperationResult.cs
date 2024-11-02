using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface ICreateUsersOperationResult
{
    ReadOnlyCollection<HandledUser> CreatedUsers { get; }
}

public class CreateUsersOperationResult : ICreateUsersOperationResult
{
    private readonly HashSet<HandledUser> _users = new();
    public ReadOnlyCollection<HandledUser> CreatedUsers => new (_users.ToArray());

    public CreateUsersOperationResult Add(HandledUser user)
    {
        
        _users.Add(user);
        return this;
    }    
    
    public CreateUsersOperationResult Add(IEnumerable<HandledUser> users)
    {
        foreach (var user in users)
        {
            _users.Add(user);
        }

        return this;
    }
}
