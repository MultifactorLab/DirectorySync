using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

internal class ModifiedUsersBucket : IModifiedUsersBucket
{
    private readonly List<IModifiedUser> _modified = [];
    public ReadOnlyCollection<IModifiedUser> ModifiedUsers => new (_modified);
    
    public ModifiedUser AddModifiedUser(MultifactorUserId id, string identity)
    {
        ArgumentNullException.ThrowIfNull(id);
        
        if (_modified.Any(x => x.Id == id))
        {
            throw new InvalidOperationException($"Modified user '{id}' already exists in this bucket");
        }

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        var user = new ModifiedUser(id, identity);
        _modified.Add(user);
        
        return user;
    }
}
