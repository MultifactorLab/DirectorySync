namespace DirectorySync.Domain.Entities;

public abstract class CachedDirectoryObject
{
    public DirectoryGuid Guid { get; }
    
    protected CachedDirectoryObject(DirectoryGuid guid)
    {
        Guid = guid ?? throw new ArgumentNullException(nameof(guid));
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        
        var compareTo = obj as CachedDirectoryObject;

        if (ReferenceEquals(this, compareTo))
        {
            return true;
        }

        if (ReferenceEquals(null, compareTo))
        {
            return false;
        }

        return Guid == compareTo.Guid;
    }

    public static bool operator ==(CachedDirectoryObject a, CachedDirectoryObject b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        {
            return true;
        }

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(CachedDirectoryObject a, CachedDirectoryObject b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return GetType().GetHashCode() * 907 + Guid.GetHashCode();
        }
    }
}
