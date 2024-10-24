using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Domain.Abstractions;

public interface IGetReferenceGroup
{
    ReferenceDirectoryGroup Execute(DirectoryGuid guid, string[] requiredAttributes);
}
