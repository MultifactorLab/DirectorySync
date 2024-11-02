using DirectorySync.Domain;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface INewUser
{
    DirectoryGuid Id { get; }
    string Identity { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}

internal class NewUser : INewUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());
    
    public string Identity { get; }

    public DirectoryGuid Id { get; }

    public NewUser(DirectoryGuid id, string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Identity = identity;
    }

    public NewUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}
