using DirectorySync.Application.Integrations.Ldap.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DirectorySync.Domain.Abstractions;

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
