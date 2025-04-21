using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Infrastructure.ConfigurationSources.Configurations;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Shared.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;

namespace DirectorySync.Infrastructure.Integrations.Multifactor.Extensions;

internal static class HostApplicationBuilderExtensions
{
    public static void AddMultifactorIntegration(this HostApplicationBuilder builder, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<MultifactorApiOptions>()
            .BindConfiguration("Multifactor")
            .ValidateDataAnnotations();

        builder.Services.TryAddTransient<HttpLogger>();
        builder.Services.TryAddTransient<MfTraceIdHeaderSetter>();
        builder.Services.AddHttpClient("MultifactorApi", (prov, cli) =>
        {
            var options = prov.GetRequiredService<IOptions<MultifactorApiOptions>>().Value;

            cli.BaseAddress = new Uri(options.Url);

            var auth = new BasicAuthHeaderValue(options.Key, options.Secret);
            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

        }).AddHttpMessageHandler<HttpLogger>()
        .AddHttpMessageHandler<MfTraceIdHeaderSetter>()
        .AddResilienceHandler("mf-api-pipeline", (resilBuilder, resilContext) =>
        {
            resilBuilder.AddRetry(ResiliencePolicy.GetDefaultRetryPolicy());

            resilBuilder.AddFallback(ResiliencePolicy.GetConflictPolicy(resilContext.ServiceProvider));
            resilBuilder.AddFallback(ResiliencePolicy.GetForbiddenPolicy());
            resilBuilder.AddFallback(ResiliencePolicy.GetUnauthorizedPolicy());

            // Defaults: https://www.pollydocs.org/strategies/timeout.html#defaults
            resilBuilder.AddTimeout(TimeSpan.FromSeconds(20));
        });

        builder.Services.AddSingleton<IMultifactorApi, MultifactorApi>();
    }
}
