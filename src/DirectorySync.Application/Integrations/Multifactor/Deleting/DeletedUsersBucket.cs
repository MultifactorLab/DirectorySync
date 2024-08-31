using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

internal class DeletedUsersBucket : IDeletedUsersBucket
{
    private readonly HashSet<MultifactorIdentity> _deleted = [];
    public ReadOnlyCollection<MultifactorIdentity> DeletedUsers => new (_deleted.ToArray());

    public int Count => _deleted.Count;

    public void Add(MultifactorIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        _deleted.Add(identity);
    }
}
