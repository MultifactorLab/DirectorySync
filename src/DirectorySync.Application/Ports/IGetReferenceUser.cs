using DirectorySync.Domain.Entities;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Application.Ports
{
    public interface IGetReferenceUser
    {
        ReferenceDirectoryUser? Execute(DirectoryGuid guid, string[] requiredAttributes);
    }
}
