using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public sealed class CloudUserModel
{
    public Identity Identity { get; }

    public DirectoryGuid? ExternalObjectId { get; }

    public CloudUserModel(Identity identity, DirectoryGuid? externalObjectId = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
        ExternalObjectId = externalObjectId;
    }
}
