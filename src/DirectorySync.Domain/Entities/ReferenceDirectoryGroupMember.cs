using System.Collections.ObjectModel;
using System.Text;

namespace DirectorySync.Domain.Entities;

public record ReferenceDirectoryGroupMember
{
    public DirectoryGuid Guid { get; }
    public ReadOnlyCollection<LdapAttribute> Attributes { get; }

    public ReferenceDirectoryGroupMember(DirectoryGuid guid, IEnumerable<LdapAttribute> attributes)
    {
        Guid = guid ?? throw new ArgumentNullException(nameof(guid));
        ArgumentNullException.ThrowIfNull(attributes);
        Attributes = new ReadOnlyCollection<LdapAttribute>(attributes.ToArray());
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder($"member: {Guid}");
        if (Attributes.Count == 0)
        {
            return sb.ToString();
        }
        
        sb.AppendLine(Environment.NewLine);
        foreach (var attribute in Attributes)
        {
            sb.AppendLine($"  attribute: {attribute}");
        }

        return sb.ToString();
    }
}