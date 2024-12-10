namespace DirectorySync.Application.Integrations.Multifactor;

internal static class MultifactorPropertyName
{
    public const string IdentityProperty = "Identity";

    public static class AdditionalProperties
    {
        public const string NameProperty = "Name";
        public const string EmailProperty = "Email";
        public const string PhoneProperty = "Phone";

        public static readonly string[] List = [ NameProperty, EmailProperty, PhoneProperty ];
    }
}
