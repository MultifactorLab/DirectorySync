using DirectorySync.Domain.Karnel;

namespace DirectorySync.Domain.ValueObjects
{
    public class GroupMappingSettings : ValueObject
    {
        public string DirectoryGroup { get; set; }
        public string[] SignUpGroups { get; set; } = Array.Empty<string>();

        private GroupMappingSettings(string directoryGroup,
            IEnumerable<string> signUpGroups)
        {
            ArgumentNullException.ThrowIfNull(directoryGroup, nameof(directoryGroup));
            ArgumentNullException.ThrowIfNull(signUpGroups, nameof(signUpGroups));

            DirectoryGroup = directoryGroup;
            SignUpGroups = signUpGroups.ToArray();
        }

        public static GroupMappingSettings Create(string directoryGroup, IEnumerable<string> signUpGroups)
        {
            return new GroupMappingSettings(directoryGroup, signUpGroups);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return DirectoryGroup;
            yield return SignUpGroups;
        }
    }
}
