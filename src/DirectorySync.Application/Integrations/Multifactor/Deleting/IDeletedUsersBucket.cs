using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeletedUsersBucket
{
    ReadOnlyCollection<MultifactorIdentity> DeletedUsers { get; } 
}
