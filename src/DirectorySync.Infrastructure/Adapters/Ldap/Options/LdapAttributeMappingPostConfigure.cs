using DirectorySync.Infrastructure.Adapters.Helpers;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Options;

internal sealed class LdapAttributeMappingPostConfigure : IPostConfigureOptions<LdapAttributeMappingOptions>
{
    public void PostConfigure(string? name, LdapAttributeMappingOptions options)
    {
        options.EmailAttributes = LdapAttributeMappingNormalizer.NormalizeOrdered(options.EmailAttributes);
        options.PhoneAttributes = LdapAttributeMappingNormalizer.NormalizeOrdered(options.PhoneAttributes);
    }
}
