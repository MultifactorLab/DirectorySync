using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto
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
    }
}
