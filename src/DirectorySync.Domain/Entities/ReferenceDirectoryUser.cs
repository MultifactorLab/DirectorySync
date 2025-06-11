using System.Text;
using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Entities;

public record ReferenceDirectoryUser
{
    public DirectoryGuid Guid { get; }
    public LdapAttributeCollection Attributes { get; }

    public ReferenceDirectoryUser(DirectoryGuid guid, LdapAttributeCollection attributes)
    {
        Guid = guid ?? throw new ArgumentNullException(nameof(guid));
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder($"member: {Guid}{Environment.NewLine}");
        sb.Append(Attributes.ToString());
        return sb.ToString();
    }
}
