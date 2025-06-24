namespace DirectorySync.Application.Models.Options;

public static class MemberPropertyOptions
{
    public const string IdentityProperty = "Identity";

    public static class AdditionalProperties
    {
        internal const string NameProperty = "Name";
        internal const string EmailProperty = "Email";
        internal const string PhoneProperty = "Phone";

        public static readonly string[] List = [ NameProperty, EmailProperty, PhoneProperty ];
    }
}
