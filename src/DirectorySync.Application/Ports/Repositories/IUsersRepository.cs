using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports.Repositories;

public interface IUsersRepository
{
    IEnumerable<User> FindUByIds(IEnumerable<Guid> ids);
    void Insert(User user);
    void Update(User user);
    void Delete(Guid id);
}
