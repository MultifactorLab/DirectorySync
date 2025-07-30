using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;

namespace DirectorySync.Application.Services;

public interface IDirectoryDomainsUpdater
{
    void UpdateDomainsToSearch(IEnumerable<LdapDomain> newDomains);
}

public class DirectoryDomainsUpdater : IDirectoryDomainsUpdater
{
    private readonly IDirectoryDomainDatabase _directoryDomainDatabase;

    public DirectoryDomainsUpdater(IDirectoryDomainDatabase directoryDomainDatabase)
    {
        _directoryDomainDatabase = directoryDomainDatabase;
    }

    public void UpdateDomainsToSearch(IEnumerable<LdapDomain> newDomains)
    {
        ArgumentNullException.ThrowIfNull(newDomains);
        
        var currentDomains = _directoryDomainDatabase.FindAll();
        
        var toDelete = currentDomains.Except(newDomains);
        var toAdd = newDomains.Except(currentDomains);
        
        _directoryDomainDatabase.DeleteMany(toDelete);
        _directoryDomainDatabase.InsertMany(toAdd);
    }
}
