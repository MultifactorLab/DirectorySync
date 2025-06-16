using System.Collections.ObjectModel;

namespace DirectorySync.Application.Models
{
    public class PropsMapping
    {
        public string IdentityAttribute { get; set; } = string.Empty;

        public ReadOnlyDictionary<string, string> AdditionalAttributes { get; set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public string NameAttribute { get; set; } = string.Empty;
        public string[] EmailAttributes { get; set; } = [];
        public string[] PhoneAttributes { get; set; } = [];
        
        public bool SendEnrollmentLink { get; set; }
        public TimeSpan EnrollmentLinkTtl { get; set; }
    }
}
