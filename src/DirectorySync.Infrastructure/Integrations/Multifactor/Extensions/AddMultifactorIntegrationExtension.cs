using DirectorySync.Application.Integrations.Multifactor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Infrastructure.Integrations.Multifactor.Extensions;

internal static class AddMultifactorIntegrationExtension
{
    public static void AddMultifactorIntegration(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IMultifactorApi, FakeMultifactorApi>();
    }
}
