using System.Collections.ObjectModel;
using DirectorySync.Application.Integrations.Multifactor.Enums;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Get;
public interface IGetUsersIdentitiesOperationResult
{
    ReadOnlyCollection<LdapIdentity> Identities { get; }
    UserNameFormat UserNameFormat { get; }
}

public class GetUsersIdentitiesOperationResult : IGetUsersIdentitiesOperationResult
{
    public ReadOnlyCollection<LdapIdentity> Identities { get; } = Array.Empty<LdapIdentity>().AsReadOnly();

    public UserNameFormat UserNameFormat { get; }

    public GetUsersIdentitiesOperationResult(IEnumerable<LdapIdentity> identities, UserNameFormat userNameFormat)
    {
        Identities = identities.ToArray().AsReadOnly();
        UserNameFormat = userNameFormat;
    }

    public GetUsersIdentitiesOperationResult() { }
}
