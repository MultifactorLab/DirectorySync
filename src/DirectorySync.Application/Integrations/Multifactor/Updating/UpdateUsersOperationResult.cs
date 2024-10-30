using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IUpdateUsersOperationResult
{
    ReadOnlyCollection<string> UpdatedUserIdentities { get; }
}

public class UpdateUsersOperationResult : IUpdateUsersOperationResult
{
    private readonly HashSet<string> _identities = new();
    public ReadOnlyCollection<string> UpdatedUserIdentities => new (_identities.ToArray());

    public UpdateUsersOperationResult Add(string identity)
    {
        _identities.Add(identity);
        return this;
    }

    public UpdateUsersOperationResult Add(IEnumerable<string> identities)
    {
        foreach (var identity in identities)
        {
            _identities.Add(identity);
        }

        return this;
    }
}
