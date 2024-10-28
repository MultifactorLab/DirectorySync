using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface ICreateUsersOperationResult
{
    ReadOnlyCollection<string> CreatedUserIdentities { get; }
}

public class CreateUsersOperationResult : ICreateUsersOperationResult
{
    private readonly HashSet<string> _createdUsers = new(StringComparer.OrdinalIgnoreCase);
    public ReadOnlyCollection<string> CreatedUserIdentities => new (_createdUsers.ToArray());

    public CreateUsersOperationResult Add(string identity)
    {
        _createdUsers.Add(identity);
        return this;
    }    
    
    public CreateUsersOperationResult Add(IEnumerable<string> identities)
    {
        foreach (var identity in identities)
        {
            _createdUsers.Add(identity);
        }

        return this;
    }
}
