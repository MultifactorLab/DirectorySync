using System.Collections.ObjectModel;
using System.Text;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Entities;

/// <summary>
/// Group from LDAP. Acts as a point of truth.
/// </summary>
public record ReferenceDirectoryGroup
{
    public DirectoryGuid Guid { get; }
    public ReadOnlyCollection<ReferenceDirectoryUser> Members { get; }

    public ReferenceDirectoryGroup(DirectoryGuid guid, IEnumerable<ReferenceDirectoryUser> members)
    {
        Guid = guid ?? throw new ArgumentNullException(nameof(guid));
        ArgumentNullException.ThrowIfNull(members);
        Members = new ReadOnlyCollection<ReferenceDirectoryUser>(members.ToArray());
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
