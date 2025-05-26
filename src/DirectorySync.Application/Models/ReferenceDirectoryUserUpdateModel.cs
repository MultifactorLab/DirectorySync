using DirectorySync.Domain;
using DirectorySync.Domain.Entities;

namespace DirectorySync.Application.Models
{
    public class ReferenceDirectoryUserUpdateModel
    {
        public DirectoryGuid Guid { get; }
        public LdapAttributeCollection Attributes { get; }
        public bool IsUnlinkedFromGroup => _isUnlinkedFromGroup;
        private bool _isUnlinkedFromGroup;

        public ReferenceDirectoryUserUpdateModel(DirectoryGuid guid, LdapAttributeCollection attributes)
        {
            Guid = guid ?? throw new ArgumentNullException(nameof(guid));
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public void UnlinkFromGroup()
        {
            _isUnlinkedFromGroup = true;
        }

        public static ReferenceDirectoryUserUpdateModel FromEntity(ReferenceDirectoryUser entity)
        {
            return new ReferenceDirectoryUserUpdateModel(entity.Guid, entity.Attributes);
        }
    }
}
