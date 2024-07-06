using System.Collections.ObjectModel;
using DirectorySync.Application.Integrations.Multifactor.Creating;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IModifiedUsersBucket
{
    ReadOnlyCollection<IModifiedUser> ModifiedUsers { get; }
}

public interface INewUsersBucket
{
    ReadOnlyCollection<INewUser> NewUsers { get; } 
}
