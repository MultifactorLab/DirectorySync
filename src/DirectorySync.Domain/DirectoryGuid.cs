using System.Text;

namespace DirectorySync.Domain;

public record DirectoryGuid
{
    public Guid Value { get; }
    public string OctetString { get; }
    
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