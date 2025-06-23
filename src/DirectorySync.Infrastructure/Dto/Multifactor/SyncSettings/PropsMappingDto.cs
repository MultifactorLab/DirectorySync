using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Options;

namespace DirectorySync.Infrastructure.Dto.Cloud.SyncSettings
{
    public class PropsMappingDto
    {
        public string IdentityAttribute { get; set; }

        public ReadOnlyDictionary<string, string> AdditionalAttributes { get; set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public string NameAttribute { get; set; }
        public string[] EmailAttributes { get; set; } = Array.Empty<string>();
        public string[] PhoneAttributes { get; set; } = Array.Empty<string>();

        public bool SendEnrollmentLink { get; set; }
        public TimeSpan EnrollmentLinkTtl { get; set; }

        public static PropsMapping ToModel(PropsMappingDto dto)
        {
            return new PropsMapping
            {
                IdentityAttribute = dto.IdentityAttribute,
                AdditionalAttributes = dto.AdditionalAttributes,
                NameAttribute = dto.NameAttribute,
                EmailAttributes = dto.EmailAttributes,
                PhoneAttributes = dto.PhoneAttributes,
                SendEnrollmentLink = dto.SendEnrollmentLink,
                EnrollmentLinkTtl = dto.EnrollmentLinkTtl,
            };
        }
    }
}
