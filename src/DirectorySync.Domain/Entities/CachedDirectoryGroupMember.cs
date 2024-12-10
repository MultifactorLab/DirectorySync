namespace DirectorySync.Domain.Entities;

public class CachedDirectoryGroupMember
{
    public DirectoryGuid Id { get; }
    public string Identity { get; }
    public AttributesHash Hash { get; private set; }
    
    public CachedDirectoryGroupMember(DirectoryGuid guid,
        string identity,
        AttributesHash hash)
    {
        Id = guid;
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
    }

    public static CachedDirectoryGroupMember Create(DirectoryGuid guid,
        string identity,
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
}
