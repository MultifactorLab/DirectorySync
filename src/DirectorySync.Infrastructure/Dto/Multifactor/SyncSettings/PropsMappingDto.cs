using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;

namespace DirectorySync.Infrastructure.Dto.Multifactor.SyncSettings
{
    public class PropsMappingDto
    {
        public string IdentityAttribute { get; set; }

        public ReadOnlyDictionary<string, string> AdditionalAttributes { get; set; } = new(new Dictionary<string, string>());

        public string NameAttribute { get; set; }
        public string[] EmailAttributes { get; set; } = [];
        public string[] PhoneAttributes { get; set; } = [];

        public bool SendEnrollmentLink { get; set; }
        public TimeSpan EnrollmentLinkTtl { get; set; }

        public static PropsMapping ToModel(PropsMappingDto dto)
        {
            return new PropsMapping
            {
                IdentityAttribute = dto.IdentityAttribute,
                NameAttribute = dto.NameAttribute,
                EmailAttributes = dto.EmailAttributes,
                PhoneAttributes = dto.PhoneAttributes
            };
        }
    }
}
