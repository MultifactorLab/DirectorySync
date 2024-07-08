using System.Collections.ObjectModel;

namespace DirectorySync.Application.Integrations.Multifactor.Creating;

internal class CreateUsersOperationResult : ICreateUsersOperationResult
{
    private readonly List<CreatedUser> _createdUsers = new();
    public ReadOnlyCollection<CreatedUser> CreatedUsers => new (_createdUsers);

    public CreateUsersOperationResult AddUser(CreatedUser user)
    {
        if (!_createdUsers.Contains(user))
        {
            _createdUsers.Add(user);
        }

        return this;
    }
}