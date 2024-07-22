using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

internal class ModifiedUser : IModifiedUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());
    public MultifactorIdentity Identity { get; }
    
    public ModifiedUser(MultifactorIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
    }

    public ModifiedUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}
