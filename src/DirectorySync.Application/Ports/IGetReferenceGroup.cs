using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports;

public interface IGetReferenceGroup
{
    ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes);
}
