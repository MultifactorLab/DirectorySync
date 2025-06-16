using DirectorySync.Application.Models.Entities;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports
{
    public interface IGetReferenceUser
    {
        ReferenceDirectoryUser? Execute(DirectoryGuid guid, string[] requiredAttributes);
    }
}
