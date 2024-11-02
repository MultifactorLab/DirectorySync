using DirectorySync.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure.Data.Extensions;

internal static class AddLiteDbStorageExtension
{
    public static void AddLiteDbStorage(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "DirectorySync");
        
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        builder.Services.Configure<LiteDbConfig>(x =>
        {
            var path = Path.Combine(dir, "storage.db");
            x.ConnectionString = $"Filename={path};Upgrade=true";
        });
        builder.Services.AddSingleton<ILiteDbConnection, LiteDbConnection>();
        builder.Services.AddSingleton<IApplicationStorage, LiteDbApplicationStorage>();
    }
}
