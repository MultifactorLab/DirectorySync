using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeletedUsersBucket
{
    ReadOnlyCollection<string> DeletedUsers { get; }
}

internal class DeletedUsersBucket : IDeletedUsersBucket
{
    private readonly HashSet<string> _deleted = [];
    public ReadOnlyCollection<string> DeletedUsers => new (_deleted.ToArray());

    public int Count => _deleted.Count;

    public void Add(string identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _deleted.Add(identity);
    }
}
