using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface INewUser
{
    DirectoryGuid Id { get; }
    string Identity { get; }
    SignUpGroupChanges SignUpGroupChanges { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}

internal class NewUser : INewUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());
    
    public string Identity { get; }

    public DirectoryGuid Id { get; }

    public SignUpGroupChanges SignUpGroupChanges { get; }

    public NewUser(DirectoryGuid id, string identity, SignUpGroupChanges signUpGroupChanges)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identity));
        }
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Identity = identity;
        SignUpGroupChanges = signUpGroupChanges;
    }

    public NewUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}
