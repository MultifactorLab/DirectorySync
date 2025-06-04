using System.Collections.ObjectModel;
using DirectorySync.Application.Integrations.Multifactor.Enums;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Get;
public interface IGetUsersIdentitiesOperationResult
{
    ReadOnlyCollection<MultifactorIdentity> Identities { get; }
    UserNameFormat UserNameFormat { get; }
}

public class GetUsersIdentitiesOperationResult : IGetUsersIdentitiesOperationResult
{
    public ReadOnlyCollection<MultifactorIdentity> Identities { get; } = new(Array.Empty<MultifactorIdentity>());
    public UserNameFormat UserNameFormat { get; } = UserNameFormat.ActiveDirectory;

    public GetUsersIdentitiesOperationResult() { }
    
    public GetUsersIdentitiesOperationResult(IEnumerable<string> identities, UserNameFormat userNameFormat)
    {
        ArgumentNullException.ThrowIfNull(identities);
        ArgumentNullException.ThrowIfNull(userNameFormat);
        
        Identities = new ReadOnlyCollection<MultifactorIdentity>(identities.Select(c => new MultifactorIdentity(c)).ToArray());
        UserNameFormat = userNameFormat;
    }
}
