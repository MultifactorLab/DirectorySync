using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Workloads;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;
using Microsoft.Extensions.Configuration;
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

        builder.Services.AddOptions<GroupMappingsOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var dtos = config.GetSection("Sync:DirectoryGroupMappings").Get<GroupMappingsDto[]>();

                options.DirectoryGroupMappings = dtos?
                    .ToDictionary(
                        x => x.DirectoryGroup,
                        x => x.SignUpGroups,
                        StringComparer.OrdinalIgnoreCase
                    ) ?? new();
            })
            .ValidateDataAnnotations();

        builder.AddCodeTimer("Logging");
    }
}

