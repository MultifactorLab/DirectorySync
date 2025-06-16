using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Integrations.Multifactor.Get;
public interface IGetUsersIdentitiesOperationResult
{
    ReadOnlyCollection<LdapIdentity> Identities { get; }
}

public class GetUsersIdentitiesOperationResult : IGetUsersIdentitiesOperationResult
{
    public ReadOnlyCollection<LdapIdentity> Identities { get; } = Array.Empty<LdapIdentity>().AsReadOnly();


    public GetUsersIdentitiesOperationResult(IEnumerable<LdapIdentity> identities)
    {
        Identities = identities.ToArray().AsReadOnly();
    }

    public GetUsersIdentitiesOperationResult() { }
}
