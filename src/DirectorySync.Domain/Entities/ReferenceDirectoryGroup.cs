using System.Collections.ObjectModel;
using System.Text;

namespace DirectorySync.Domain.Entities;

/// <summary>
/// Group from LDAP. Acts as a point of truth.
/// </summary>
public record ReferenceDirectoryGroup
{
    public DirectoryGuid Guid { get; }
    public ReadOnlyCollection<ReferenceDirectoryGroupMember> Members { get; }

    public ReferenceDirectoryGroup(DirectoryGuid guid, IEnumerable<ReferenceDirectoryGroupMember> members)
    {
        Guid = guid ?? throw new ArgumentNullException(nameof(guid));
        ArgumentNullException.ThrowIfNull(members);
        Members = new ReadOnlyCollection<ReferenceDirectoryGroupMember>(members.ToArray());
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"group: {Guid}{Environment.NewLine}");
        sb.Append($"Members: {Members.Count}");
        
        if (Members.Count == 0)
        {
            return sb.ToString();
        }
        
        sb.AppendLine();
        foreach (var member in Members.Take(10))
        {
            sb.AppendLine($"  member: {member.Guid}");
        }

        if (Members.Count > 10)
        {
            sb.AppendLine($"  ...and {Members.Count - 10} more members");
        }
        
        return sb.ToString();
    }
}
