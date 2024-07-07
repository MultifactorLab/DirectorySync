using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Workloads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Application.Extensions;

public static class AddApplicationServicesExtension
{
    public static void AddApplicationServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SynchronizeExistedUsers>();
        builder.Services.AddSingleton<HandleNewUsers>();
        
        builder.Services.AddSingleton<GetReferenceGroupByGuid>();
        builder.Services.AddOptions<LdapOptions>()
            .BindConfiguration("Ldap")
            .ValidateDataAnnotations();
        
        builder.Services.AddSingleton<RequiredLdapAttributes>();
        builder.Services.AddSingleton<MultifactorPropertyMapper>();
        
        builder.Services.AddOptions<LdapAttributeMappingOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
        
        builder.AddMultifactorIntegration();
    }

    private static void AddMultifactorIntegration(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMultifactorApi>();
    }
}
