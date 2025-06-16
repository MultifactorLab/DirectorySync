using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core
{
    public class MemberModel : BaseModel
    {
        public Identity Identity { get; }
        
        public LdapAttributeCollection Attributes { get; }
        public AttributesHash AttributesHash { get; private set; }
        
        private readonly List<DirectoryGuid> _groupIds = new();
        public ReadOnlyCollection<DirectoryGuid> GroupIds => _groupIds.AsReadOnly();

        private MemberModel(DirectoryGuid id,
            Identity identity,
            LdapAttributeCollection attributes,
            IEnumerable<DirectoryGuid> groupIds) : base(id)
        {
            ArgumentNullException.ThrowIfNull(identity, nameof(identity));
            ArgumentNullException.ThrowIfNull(attributes, nameof(attributes));
            ArgumentNullException.ThrowIfNull(groupIds, nameof(groupIds));
            
            Identity = identity;
            Attributes = attributes;
            AttributesHash = new AttributesHash(attributes);
            _groupIds = groupIds.ToList();
        }

        public static MemberModel Create(Guid id,
            Identity identity,
            LdapAttributeCollection attributes,
            IEnumerable<DirectoryGuid> groupIds)
        {
            return new MemberModel(id, identity, attributes, groupIds);
        }

        public void AddGroups(IEnumerable<DirectoryGuid> groupIds)
        {
            ArgumentNullException.ThrowIfNull(groupIds);

            var duplicates  = _groupIds.Intersect(groupIds).ToArray();
            if (duplicates.Length != 0)
            {
                var joined = $"Groups already assigned: {string.Join(", ", duplicates.Select(d => d.Value))}";
                throw new InvalidOperationException(joined);
            }

            _groupIds.AddRange(groupIds);
        }

        public void RemoveGroups(IEnumerable<DirectoryGuid> groupIds)
        {
            ArgumentNullException.ThrowIfNull(groupIds);

            _groupIds.RemoveAll(x => groupIds.Contains(x));
        }

        public void UpdateHash(AttributesHash newHash)
        {
            ArgumentNullException.ThrowIfNull(newHash);
            if (AttributesHash != newHash)
            {
                AttributesHash = newHash;
            }
        }
    }
}
