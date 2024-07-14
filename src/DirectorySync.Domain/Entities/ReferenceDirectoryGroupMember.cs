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
        var sb = new StringBuilder($"member: {Guid}{Environment.NewLine}");
        sb.Append($"Attributes: {Attributes.Count}");
        
        if (Attributes.Count == 0)
        {
            return sb.ToString();
        }
        
        sb.AppendLine();
        foreach (var attribute in Attributes.Take(10))
        {
            sb.AppendLine($"  attribute: {attribute}");
        }
        
        if (Attributes.Count > 10)
        {
            sb.AppendLine($"  ...and {Attributes.Count - 10} more attributes");
        }

        return sb.ToString();
    }
}
