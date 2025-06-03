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
    private readonly HashSet<MultifactorIdentity> _identites = new();
    public ReadOnlyCollection<MultifactorIdentity> Identities => new(_identites.ToArray());

    private UserNameFormat _userNameFormat = UserNameFormat.ActiveDirectory;
    public UserNameFormat UserNameFormat => _userNameFormat;

    public GetUsersIdentitiesOperationResult Add(string identity)
    {
        var mfIdentity = FormatIdentity(identity, _userNameFormat);
        _identites.Add(mfIdentity);
        return this;
    }

    public GetUsersIdentitiesOperationResult Add(IEnumerable<string> identities)
    {
        var mfIdentities = identities
            .Select(identity => FormatIdentity(identity, _userNameFormat))
            .Where(mfIdentity => mfIdentity is not null);

        foreach (var identity in mfIdentities)
        {

            _identites.Add(identity);
        }

        return this;
    }

    public GetUsersIdentitiesOperationResult SetUserNameFormat(UserNameFormat userNameFormat)
    {
        _userNameFormat = userNameFormat;
        return this;
    }

    private MultifactorIdentity FormatIdentity(string identity, UserNameFormat userNameFormat)
    {
        return userNameFormat switch
        {
            UserNameFormat.Identity => MultifactorIdentity.FromRawString(identity),
            UserNameFormat.ActiveDirectory => MultifactorIdentity.FromLdapFormat(identity),
            _ => throw new NotImplementedException(userNameFormat.ToString())
        };
    }
}
