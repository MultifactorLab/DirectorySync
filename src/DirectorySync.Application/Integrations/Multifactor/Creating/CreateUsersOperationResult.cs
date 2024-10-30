using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface ICreateUsersOperationResult
{
    ReadOnlyCollection<string> CreatedUserIdentities { get; }
}

public class CreateUsersOperationResult : ICreateUsersOperationResult
{
    private readonly HashSet<string> _identities = new();
    public ReadOnlyCollection<string> CreatedUserIdentities => new (_identities.ToArray());

    public CreateUsersOperationResult Add(string identity)
    {
        _identities.Add(identity);
        return this;
    }    
    
    public CreateUsersOperationResult Add(IEnumerable<string> identities)
    {
        foreach (var identity in identities)
        {
            _identities.Add(identity);
        }

        return this;
    }
}
