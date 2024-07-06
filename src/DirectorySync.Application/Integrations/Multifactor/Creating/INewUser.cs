using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface INewUser
{
    string Identity { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}
