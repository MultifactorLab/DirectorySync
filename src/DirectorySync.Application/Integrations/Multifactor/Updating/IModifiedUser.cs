using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IModifiedUser
{
    MultifactorUserId Id { get; }
    string Identity { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}
