using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Shared.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
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
        .AddResilienceHandler("mf-api-pipeline", x =>
        {
            // Defaults: https://www.pollydocs.org/strategies/retry.html#defaults
            x.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
            });

            // Defaults: https://www.pollydocs.org/strategies/timeout.html#defaults
            x.AddTimeout(TimeSpan.FromSeconds(5));
        });

        builder.Services.AddSingleton<IMultifactorApi, MultifactorApi>();
    }
}
