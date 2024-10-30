namespace DirectorySync.Domain.Entities;

public class CachedDirectoryGroupMember : CachedDirectoryObject
{
    public MultifactorIdentity Identity { get; }
    public AttributesHash Hash { get; private set; }
    public bool Propagated { get; private set; }
    
    public CachedDirectoryGroupMember(DirectoryGuid guid, 
        MultifactorIdentity identity,
        AttributesHash hash) 
        : base(guid)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
    }

    public static CachedDirectoryGroupMember Create(DirectoryGuid guid,
        MultifactorIdentity identity,
        LdapAttributeCollection attributes)
    {
        ArgumentNullException.ThrowIfNull(guid);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(attributes);

        var hash = new AttributesHash(attributes);
        return new CachedDirectoryGroupMember(guid, identity, hash);
    }

    public void UpdateHash(AttributesHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (Hash != hash)
        {
            Hash = hash;
        }
    }

    public void Propagate()
    {
        Propagated = true;
    }
}
