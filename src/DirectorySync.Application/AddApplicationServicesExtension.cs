using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Workloads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Application;

public static class AddApplicationServicesExtension
{
    public static void AddApplicationServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddOptions<UserProcessingOptions>()
            .BindConfiguration("UserProcessing")
            .ValidateDataAnnotations();

        builder.Services.AddSingleton<Deleter>();
        builder.Services.AddSingleton<Updater>();

        builder.Services.AddSingleton<ISynchronizeUsers, SynchronizeUsers>();
        builder.Services.AddSingleton<IScanUsers, ScanUsers>();

        builder.Services.AddSingleton<RequiredLdapAttributes>();
        builder.Services.AddSingleton<MultifactorPropertyMapper>();

        builder.Services.AddOptions<LdapAttributeMappingOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
    }
}

