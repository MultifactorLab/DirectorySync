using DirectorySync.Application.Measuring;
using DirectorySync.Application.Services;
using DirectorySync.Application.UseCases;
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

        builder.Services.AddTransient<IUserCreator, UserCreator>();
        builder.Services.AddTransient<IUserUpdater, UserUpdater>();
        builder.Services.AddTransient<IUserDeleter, UserDeleter>();
        
        builder.Services.AddTransient<ISynchronizeUsersUseCase, SynchronizeUsersUseCase>();
        builder.Services.AddTransient<ISynchronizeGroupsUseCase, SynchronizeGroupsUseCase>();
        builder.Services.AddTransient<IInitialSynchronizeUsersUseCase, InitialSynchronizeUsersUseCase>();
        builder.Services.AddTransient<ISynchronizeCloudSettingsUseCase, SynchronizeCloudSettingsUseCase>();
        
        builder.AddCodeTimer("Logging");
    }
}

