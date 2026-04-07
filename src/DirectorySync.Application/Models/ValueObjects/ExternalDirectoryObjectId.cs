namespace DirectorySync.Application.Models.ValueObjects;

/// <summary>
/// An immutable identifier of an object in the catalog (for example, AD <c>objectGUID</c>) is transferred to the cloud for matching records when changing the username.
/// </summary>
public static class ExternalDirectoryObjectId
{
    public static string ToCanonicalString(DirectoryGuid directoryGuid)
    {
        ArgumentNullException.ThrowIfNull(directoryGuid);
        return directoryGuid.Value.ToString("D");
    }
}
