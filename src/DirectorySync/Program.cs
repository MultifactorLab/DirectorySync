using System.Runtime.InteropServices;
using DirectorySync.Application;
using DirectorySync.Application.Extensions;
using DirectorySync.Application.Measuring;
using DirectorySync.Extensions;
using DirectorySync.Infrastructure;
using DirectorySync.Infrastructure.Data.Extensions;
using DirectorySync.Infrastructure.Logging;

var builder = Host.CreateApplicationBuilder(args);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    throw new PlatformNotSupportedException("Only for Windows platform");
}

builder.Configuration.AddEnvironmentVariables("DIRECTORYSYNC");
if (builder.Environment.EnvironmentName == "localhost")
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = Literals.ServiceName;
});

builder.RegisterLogger();


builder.AddApplicationServices();
builder.AddLiteDbStorage();
builder.AddHostedServices();
builder.AddCodeTimer("Logging");


var host = builder.Build();
host.RegisterApplicationHostEventsLogging();

host.Run();
