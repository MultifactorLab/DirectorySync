namespace DirectorySync.Domain.Entities;

public class CachedDirectoryGroupMember : CachedDirectoryObject
{
    public AttributesHash Hash { get; private set; }
    public MultifactorUserId UserId { get; }
    
    public CachedDirectoryGroupMember(DirectoryGuid guid,
        AttributesHash hash,
        MultifactorUserId userId) 
        : base(guid)
    {
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    public void UpdateHash(AttributesHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (Hash != hash)
        {
            Hash = hash;
        }

        Modified = true;
    }
}
