using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

public interface ICreateUsersOperationResult
{
    ReadOnlyCollection<CreatedUser> CreatedUsers { get; }
}
