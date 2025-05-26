using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Ports
{
    public interface IGetReferenceUser
    {
        ReferenceDirectoryUser? Execute(DirectoryGuid guid, string[] requiredAttributes);
    }
}
