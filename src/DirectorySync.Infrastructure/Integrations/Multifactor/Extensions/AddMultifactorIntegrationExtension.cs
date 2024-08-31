using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Integrations.Multifactor.Extensions;

internal static class AddMultifactorIntegrationExtension
{
    public static void AddMultifactorIntegration(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<MultifactorApiOptions>()
            .BindConfiguration("Multifactor")
            .ValidateDataAnnotations();

        builder.Services.TryAddTransient<HttpLogger>();
        builder.Services.AddHttpClient("MultifactorApi", (prov, cli) =>
        {
            var options = prov.GetRequiredService<IOptions<MultifactorApiOptions>>().Value;

            cli.BaseAddress = new Uri(options.Url);

            var auth = new BasicAuthHeaderValue(options.Key, options.Secret);
            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

        }).AddHttpMessageHandler<HttpLogger>().AddHttpMessageHandler<MfTraceIdHeaderSetter>();

        builder.Services.AddSingleton<IMultifactorApi, FakeMultifactorApi>();
    }
}
