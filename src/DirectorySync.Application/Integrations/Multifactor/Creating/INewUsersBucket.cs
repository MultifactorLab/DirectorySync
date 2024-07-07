using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface INewUsersBucket
{
    ReadOnlyCollection<INewUser> NewUsers { get; } 
}
