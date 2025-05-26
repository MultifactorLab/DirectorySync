using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Domain;
using System.Collections.ObjectModel;

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
    
    public ModifiedUser Add(DirectoryGuid id, string identity, SignUpGroupChanges signUpGroupChanges)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        ArgumentNullException.ThrowIfNull(identity);
        
        if (_modified.Any(x => x.Id == id))
        {
            throw new InvalidOperationException($"Modified user {{{id}, {identity}}} already exists in this bucket");
        }

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }

        var user = new ModifiedUser(id, identity, signUpGroupChanges);
        _modified.Add(user);
        
        return user;
    }
}
