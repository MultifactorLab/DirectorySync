using System.Runtime.InteropServices;
using DirectorySync.Application;
using DirectorySync.Application.Integrations.Ldap.Windows;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.Data;
using DirectorySync.Infrastructure.Logging;
using DirectorySync.Services;

var builder = Host.CreateApplicationBuilder(args);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    throw new PlatformNotSupportedException("Only for Windows platform");
}

builder.Configuration.AddEnvironmentVariables("DIRECTORYSYNC");

var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var dir = Path.Combine(localAppData, "DirectorySync");

if (builder.Environment.EnvironmentName == "localhost")
{
    builder.Configuration.AddUserSecrets<Program>();
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }
}

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = Literals.ServiceName;
});

builder.RegisterLogger();

builder.Services.Configure<LiteDbConfig>(x =>
{
    var path = Path.Combine(dir, "storage.db");
    x.ConnectionString = $"Filename={path};Upgrade=true";
});
builder.Services.AddSingleton<LiteDbConnection>();
builder.Services.AddSingleton<IApplicationStorage, LiteDbApplicationStorage>();

builder.Services.AddSingleton<GetReferenceGroupByGuid>();
builder.Services.AddOptions<LdapOptions>()
    .BindConfiguration("Ldap")
    .ValidateDataAnnotations();

builder.Services.AddSingleton<WorkloadState>();
builder.Services.AddHostedService<UserSynchronizer>();
builder.Services.AddHostedService<NewUserHandler>();
builder.Services.AddOptions<SyncOptions>()
    .BindConfiguration("Sync")
    .ValidateDataAnnotations();

builder.Services.AddSingleton<SynchronizeExistedUsers>();
builder.Services.AddSingleton<RequiredLdapAttributes>();
builder.Services.AddSingleton<MultifactorPropertyMapper>();
builder.Services.AddOptions<LdapAttributeMappingOptions>()
    .BindConfiguration("Sync")
    .ValidateDataAnnotations();

var host = builder.Build();
host.Run();
