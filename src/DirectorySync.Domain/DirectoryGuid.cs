using System.Text;

namespace DirectorySync.Domain;

public record DirectoryGuid
{
    public Guid Value { get; }
    public string OctetString { get; }
    
    /// <summary>
    /// Creates DirectoryGuid from the base .net <see cref="Guid"/> structure.
    /// </summary>
    /// <param name="guid">Guid value.</param>
    /// <exception cref="Exception"></exception>
    public DirectoryGuid(Guid guid)
    {
        if (guid == Guid.Empty)
        {
            throw new Exception("Invalid directory guid");
        }
        
        Value = guid;

        var sb = new StringBuilder();
        foreach (var t in guid.ToByteArray())
        {
            sb.Append($"\\{t.ToString("X2")}");
        }
        
        OctetString = sb.ToString();
    }

    /// <summary>
    /// Creates DirectoryGuid from the bytes representation of the base .net <see cref="Guid"/> structure.
    /// </summary>
    /// <param name="guidBytes">Guid butes.</param>
    public DirectoryGuid(byte[] guidBytes) : this(new Guid(guidBytes))
    {
        ArgumentNullException.ThrowIfNull(guidBytes);
    }
    
    public static implicit operator DirectoryGuid(Guid guid)
    {
        return new(guid);
    }
    
    public static implicit operator string(DirectoryGuid directoryGuid)
    {
        if (directoryGuid is null)
        {
            throw new InvalidCastException("Directory guid is null");
        }

        return directoryGuid.Value.ToString();
    }
    
    public static implicit operator Guid(DirectoryGuid directoryGuid)
    {
        if (directoryGuid is null)
        {
            throw new InvalidCastException("Directory guid is null");
        }

        return directoryGuid.Value;
    }

    public override string ToString() => Value.ToString();
}
