using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Databases;

public interface IDirectoryDomainDatabase
{
    ReadOnlyCollection<LdapDomain> FindAll();
    void InsertMany(IEnumerable<LdapDomain> domains);
    void DeleteMany(IEnumerable<LdapDomain> domains);
}
