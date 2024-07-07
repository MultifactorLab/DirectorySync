using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IModifiedUsersBucket
{
    ReadOnlyCollection<IModifiedUser> ModifiedUsers { get; }
}
