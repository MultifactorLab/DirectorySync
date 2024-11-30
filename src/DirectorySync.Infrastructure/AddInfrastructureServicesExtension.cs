using DirectorySync.Infrastructure.Data.Extensions;
using DirectorySync.Infrastructure.Integrations.Ldap.Extensions;
using DirectorySync.Infrastructure.Integrations.Multifactor.Extensions;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure
{
    public static class AddInfrastructureServicesExtension
    {
        public static void AddInfrastructureServices(this HostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddLdapIntegration();
            builder.AddLiteDbStorage();
            builder.AddMultifactorIntegration();
        }
    }
}
