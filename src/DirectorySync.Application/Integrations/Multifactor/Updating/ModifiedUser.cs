using System.Collections.ObjectModel;
using DirectorySync.Application.Integrations.Multifactor.Models;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IModifiedUser
{
    DirectoryGuid Id { get; }
    string Identity { get; }
    SignUpGroupChanges SignUpGroupChanges { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}

internal class ModifiedUser : IModifiedUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());

    public DirectoryGuid Id { get; }
    public string Identity { get; }
    public SignUpGroupChanges SignUpGroupChanges { get; }

    public ModifiedUser(DirectoryGuid id, string identity, SignUpGroupChanges signUpGroupChanges)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Identity = identity;
        SignUpGroupChanges = signUpGroupChanges;
    }

    public ModifiedUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}
