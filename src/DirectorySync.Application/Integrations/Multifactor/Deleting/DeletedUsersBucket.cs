using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeletedUsersBucket
{
    ReadOnlyCollection<IDeletedUser> DeletedUsers { get; }
}

internal class DeletedUsersBucket : IDeletedUsersBucket
{
    private readonly List<IDeletedUser> _deleted = [];
    public ReadOnlyCollection<IDeletedUser> DeletedUsers => new (_deleted);

    public int Count => _deleted.Count;

    public DeletedUser Add(DirectoryGuid id, string identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (_deleted.Any(x => x.Id == id))
        {
            throw new InvalidOperationException($"User {{{id}, {identity}}} already exists in this bucket");
        }

        var user = new DeletedUser(id, identity);
        _deleted.Add(user);

        return user;
    }
}
