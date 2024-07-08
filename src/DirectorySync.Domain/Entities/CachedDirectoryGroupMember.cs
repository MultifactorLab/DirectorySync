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
        AttributesHash hash) 
        : base(guid)
    {
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        UserId = MultifactorUserId.Undefined;
    }

    public static CachedDirectoryGroupMember Create(DirectoryGuid guid,
        IEnumerable<LdapAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(attributes);

        var hash = new AttributesHash(attributes);
        return new CachedDirectoryGroupMember(guid, hash);
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

    public void SetUserId(MultifactorUserId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (UserId != MultifactorUserId.Undefined)
        {
            throw new InvalidOperationException("Cached member already has a user id");
        }
        
        UserId = id;
        Modified = true;
    }

    public void Commit()
    {
        Modified = false;
    }
}
