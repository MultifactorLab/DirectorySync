using DirectorySync.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure
{
    public static class AddInfrastructureServicesExtension
    {
        public static void AddInfrastructureServices(this HostApplicationBuilder builder, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddMultifactorAdapter();
            builder.AddLiteDbAdapter();
            builder.AddSyncOptions();
        }
    }
}
