using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Measuring;
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

        builder.Services.AddTransient<Creator>();
        builder.Services.AddTransient<Updater>();
        builder.Services.AddTransient<Deleter>();

        builder.Services.AddTransient<ISynchronizeUsers, SynchronizeUsers>();
        builder.Services.AddTransient<IScanUsers, ScanUsers>();

        builder.Services.AddTransient<RequiredLdapAttributes>();

        builder.Services.AddOptions<LdapAttributeMappingOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();

        builder.Services.AddTransient<TrackingGroupsMapping>();

        builder.Services.AddOptions<GroupMappingsOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();


        builder.AddCodeTimer("Logging");
    }
}

