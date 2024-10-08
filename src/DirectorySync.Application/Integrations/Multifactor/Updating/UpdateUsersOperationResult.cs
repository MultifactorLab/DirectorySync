using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IUpdateUsersOperationResult
{
    ReadOnlyCollection<MultifactorIdentity> UpdatedUsers { get; }
}

public class UpdateUsersOperationResult : IUpdateUsersOperationResult
{
    private readonly HashSet<MultifactorIdentity> _updatedUsers = new();
    public ReadOnlyCollection<MultifactorIdentity> UpdatedUsers => new (_updatedUsers.ToArray());

    public UpdateUsersOperationResult AddUserId(MultifactorIdentity userId)
    {
        _updatedUsers.Add(userId);
        return this;
    }
}
