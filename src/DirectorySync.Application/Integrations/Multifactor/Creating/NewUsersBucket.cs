using DirectorySync.Domain;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface INewUsersBucket
{
    ReadOnlyCollection<INewUser> NewUsers { get; }
}

internal class NewUsersBucket : INewUsersBucket
{
    private readonly List<INewUser> _newUsers = [];
    public ReadOnlyCollection<INewUser> NewUsers => new (_newUsers);
    
    public NewUser AddNewUser(DirectoryGuid id, string identity)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        
        if (_newUsers.Any(x => x.Id == id))
        {
            throw new InvalidOperationException($"User {{{id}, {identity}}} already exists in this bucket");
        }
        
        var user = new NewUser(id, identity);
        _newUsers.Add(user);

        return user;
    }

    public void Clear() => _newUsers.Clear();
}
