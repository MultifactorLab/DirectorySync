using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

internal class NewUsersBucket : INewUsersBucket
{
    private readonly List<INewUser> _newUsers = [];
    public ReadOnlyCollection<INewUser> NewUsers => new (_newUsers);
    
    public NewUser AddNewUser(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        
        if (_newUsers.Any(x => x.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"User '{identity}' already exists in this bucket");
        }
        
        var user = new NewUser(identity);
        _newUsers.Add(user);

        return user;
    }

    public void Clear() => _newUsers.Clear();
}
