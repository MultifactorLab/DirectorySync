namespace DirectorySync.Domain.Entities;

public class CachedDirectoryGroupMember : CachedDirectoryObject
{
    public AttributesHash Hash { get; private set; }
    public MultifactorUserId UserId { get; private set; }
    public bool Modified { get; private set; }
    
    public CachedDirectoryGroupMember(DirectoryGuid guid,
        AttributesHash hash,
        MultifactorUserId userId) 
        : base(guid)
    {
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    }
    
    private CachedDirectoryGroupMember(DirectoryGuid guid,
        MultifactorUserId userId,
        AttributesHash hash) 
        : base(guid)
    {
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        UserId = userId;
    }

    public static CachedDirectoryGroupMember Create(DirectoryGuid guid, MultifactorUserId userId,
        IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(attributes);

        var hash = new AttributesHash(attributes);
        return new CachedDirectoryGroupMember(guid, userId, hash);
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

    public void Commit()
    {
        Modified = false;
    }
}
