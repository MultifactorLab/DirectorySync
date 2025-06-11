using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Entities
{
    public class GroupSnapshot
    {
        public Guid GroupId { get; }
        public EntriesHash Hash { get; }
        public long LastSeenUsn { get; }
    }
}
