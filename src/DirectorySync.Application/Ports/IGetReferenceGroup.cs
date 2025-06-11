using DirectorySync.Domain.Entities;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Application.Ports;

public interface IGetReferenceGroup
{
    ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes);
}
