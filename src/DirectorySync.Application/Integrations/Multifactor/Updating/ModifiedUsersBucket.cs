using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IModifiedUsersBucket
{
    ReadOnlyCollection<IModifiedUser> ModifiedUsers { get; }
}

internal class ModifiedUsersBucket : IModifiedUsersBucket
{
    private readonly List<IModifiedUser> _modified = [];
    public ReadOnlyCollection<IModifiedUser> ModifiedUsers => new (_modified);

    public int Count => _modified.Count;
    
    public ModifiedUser Add(MultifactorIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        
        if (_modified.Any(x => x.Identity == identity))
        {
            throw new InvalidOperationException($"Modified user '{identity}' already exists in this bucket");
        }

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        var user = new ModifiedUser(identity);
        _modified.Add(user);
        
        return user;
    }
}
