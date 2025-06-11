using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports.Repositories;

public interface IGroupsRepository
{
    IEnumerable<Group> FindUByIds(IEnumerable<Guid> ids);
    void Insert(Group group);
    void Update(Group group);
    void Delete(Guid id);
}
