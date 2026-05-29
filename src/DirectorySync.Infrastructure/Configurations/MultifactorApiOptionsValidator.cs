using DirectorySync.Infrastructure.Adapters.Multifactor;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Configurations;

internal sealed class MultifactorApiOptionsValidator : IValidateOptions<MultifactorApiOptions>
{
    public ValidateOptionsResult Validate(string? name, MultifactorApiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Url)
            || string.IsNullOrWhiteSpace(options.Key)
            || string.IsNullOrWhiteSpace(options.Secret))
        {
            return ValidateOptionsResult.Fail(
                "Multifactor:Url, Multifactor:Key, and Multifactor:Secret must be non-empty. Set them in appsettings.json or DIRECTORYSYNC_* environment variables.");
        }

        if (!Uri.TryCreate(options.Url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                "Multifactor:Url must be an absolute HTTP or HTTPS URL.");
        }

        return ValidateOptionsResult.Success;
    }
}
