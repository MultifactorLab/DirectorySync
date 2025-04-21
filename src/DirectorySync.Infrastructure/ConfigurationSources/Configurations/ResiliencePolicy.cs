using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DirectorySync.Infrastructure.Common.Dto;
using DirectorySync.Infrastructure.ConfigurationSources.MultifactorCloud;
using DirectorySync.Infrastructure.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Fallback;

namespace DirectorySync.Infrastructure.ConfigurationSources.Configurations
{
    internal static class ResiliencePolicy
    {
        internal static HttpRetryStrategyOptions GetDefaultRetryPolicy()
        {
            // Defaults: https://www.pollydocs.org/strategies/retry.html#defaults
            return new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
            };
        }

        internal static FallbackStrategyOptions<HttpResponseMessage> GetForbiddenPolicy()
        {
            return new FallbackStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(response => response.StatusCode == HttpStatusCode.Forbidden),
                FallbackAction = async args =>
                {
                    var errorMessage = await GetHttpResponseErrorMessage(args.Outcome.Result, args.Context.CancellationToken);

                    var message = $"Fallback after failed attempt for 403 (Forbidden).\n{errorMessage ?? "No details"} " +
                         "\nThrowing ForbiddenException... Please check log file and \"Accounts Synchronization\" section settings in the Multifactor Cloud.";

                    throw new ForbiddenException(message);
                }
            };
        }

        internal static FallbackStrategyOptions<HttpResponseMessage> GetConflictPolicy(IServiceProvider serviceProvider)
        {
            return new FallbackStrategyOptions<HttpResponseMessage>()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode == HttpStatusCode.Conflict),
                FallbackAction = async args =>
                {
                    var isDsEnable = CheckActualDsSettings(serviceProvider);

                    if (!isDsEnable)
                    {
                        var errorMessage = await GetHttpResponseErrorMessage(args.Outcome.Result, args.Context.CancellationToken);

                        var message = $"Fallback after failed attempt for 409 (Conflict).\n{errorMessage ?? "No details"} " +
                             "\nThrowing ConflictException... Please check log file and \"Accounts Synchronization\" section settings in the Multifactor Cloud.";

                        throw new ConflictException(message);
                    }
                    return default;
                }
            };
        }

        internal static FallbackStrategyOptions<HttpResponseMessage> GetUnauthorizedPolicy()
        {
            return new FallbackStrategyOptions<HttpResponseMessage>()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode == HttpStatusCode.Unauthorized),
                FallbackAction = async args =>
                {
                    var errorMessage = await GetHttpResponseErrorMessage(args.Outcome.Result, args.Context.CancellationToken);

                    var message = $"Fallback after failed attempt for 401 (Unauthorized).\n{errorMessage ?? "No details"} " +
                            "\nThrowing UnauthorizedException... Please check log file and valid credentials in the \"Accounts Synchronization\" section of the Multifactor Cloud.";

                    throw new UnauthorizedException(message);
                }
            };
        }

        private static bool CheckActualDsSettings(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();

            var provider = configuration.Providers
                .OfType<MultifactorCloudConfigurationSource>()
                .FirstOrDefault();

            if (provider != null)
            {
                provider.Refresh(null);
            }

            bool enabled = configuration.GetValue<bool>("Sync:Enabled");
            return enabled;
        }

        private async static Task<string?> GetHttpResponseErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string? errorMessage = null;

            try
            {
                if (response?.Content != null)
                {
                    var options = new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    var unsuccessfulResponse = await response.Content.ReadFromJsonAsync<UnsuccessfulResponse>(options, cancellationToken);

                    if (unsuccessfulResponse is not null && !string.IsNullOrEmpty(unsuccessfulResponse.Message))
                    {
                        errorMessage = Regex.Unescape(unsuccessfulResponse.Message);
                    }
                }

                return errorMessage;
            }
            catch
            {
                return errorMessage;
            }
        }
    }
}
