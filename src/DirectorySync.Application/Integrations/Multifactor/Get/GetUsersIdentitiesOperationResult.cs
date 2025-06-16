using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Integrations.Multifactor.Get;
public interface IGetUsersIdentitiesOperationResult
{
    ReadOnlyCollection<Identity> Identities { get; }
}

public class GetUsersIdentitiesOperationResult : IGetUsersIdentitiesOperationResult
{
    public ReadOnlyCollection<Identity> Identities { get; } = Array.Empty<Identity>().AsReadOnly();


    public GetUsersIdentitiesOperationResult(IEnumerable<Identity> identities)
    {
        Identities = identities.ToArray().AsReadOnly();
    }

    public GetUsersIdentitiesOperationResult() { }
}
