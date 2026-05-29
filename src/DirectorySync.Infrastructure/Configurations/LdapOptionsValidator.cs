using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using DirectorySync.Infrastructure.Shared.Multifactor.Core.Ldap;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Configurations;

internal sealed class LdapOptionsValidator : IValidateOptions<LdapOptions>
{
    private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromHours(1);

    public ValidateOptionsResult Validate(string? name, LdapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Path)
            || string.IsNullOrWhiteSpace(options.Username)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail(
                "Ldap:Path, Ldap:Username, and Ldap:Password must be non-empty. Set them in appsettings.json or DIRECTORYSYNC_* environment variables.");
        }

        try
        {
            _ = new LdapConnectionString(options.Path.Trim());
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail($"Ldap:Path is not a valid LDAP connection string: {ex.Message}");
        }

        if (options.Timeout < MinTimeout || options.Timeout > MaxTimeout)
        {
            return ValidateOptionsResult.Fail(
                $"Ldap:Timeout must be between {MinTimeout.TotalSeconds} and {MaxTimeout.TotalHours} hours.");
        }

        return ValidateOptionsResult.Success;
    }
}
