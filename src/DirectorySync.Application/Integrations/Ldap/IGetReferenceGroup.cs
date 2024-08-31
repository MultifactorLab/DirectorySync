using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Integrations.Ldap;

public interface IGetReferenceGroup
{
    ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes);
}
