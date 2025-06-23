using DirectorySync.Infrastructure.Integrations.Ldap.Extensions;
using DirectorySync.Infrastructure.Integrations.Multifactor.Extensions;
using Microsoft.Extensions.Hosting;
using HostApplicationBuilderExtensions = DirectorySync.Infrastructure.Extensions.HostApplicationBuilderExtensions;

namespace DirectorySync.Infrastructure
{
    public static class AddInfrastructureServicesExtension
    {
        public static void AddInfrastructureServices(this HostApplicationBuilder builder, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(builder);
            
            builder.AddLdapIntegration(args);
            builder.AddLiteDbStorage(args);
            builder.AddMultifactorIntegration(args);
            HostApplicationBuilderExtensions.AddMultifactorAdapter(builder);
            HostApplicationBuilderExtensions.AddSyncOptions(builder);
        }
    }
}
