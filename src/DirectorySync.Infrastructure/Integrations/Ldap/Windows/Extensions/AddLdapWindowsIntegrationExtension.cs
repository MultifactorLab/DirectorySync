using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Ldap;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DirectorySync.Infrastructure.Integrations.Ldap.Windows.Extensions
{
    internal static class AddLdapWindowsIntegrationExtension
    {
        public static void AddLdapWindowsIntegration(this HostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<IGetReferenceGroup, GetReferenceGroupWithDirectorySearcher>();
            builder.Services.AddOptions<LdapOptions>()
                .BindConfiguration("Ldap")
                .ValidateDataAnnotations();
        }
    }
}
