using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

internal class ModifiedUser : IModifiedUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());
    public MultifactorUserId Id { get; }
    
    public string Identity { get; }
    
    public ModifiedUser(MultifactorUserId id, string identity)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (id == MultifactorUserId.Undefined)
        {
            throw new ArgumentException("User id cannot be undefined", nameof(id));
        }
        
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        Identity = identity;
    }

    public ModifiedUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}
