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
        var sb = new StringBuilder($"group: {Guid}");
        if (Members.Count == 0)
        {
            return sb.ToString();
        }
        
        sb.AppendLine(Environment.NewLine);
        foreach (var member in Members)
        {
            sb.AppendLine($"  member: {member.Guid}");
        }
        return sb.ToString();
    }
}
