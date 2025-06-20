using Microsoft.Extensions.Configuration;

namespace DirectorySync.Infrastructure.ConfigurationSources.SystemEnvironmentVariables;

/// <summary>
/// Represents SYSTEM (Machine level) environment variables as an <see cref="IConfigurationSource"/>.
/// </summary>
public sealed class SystemEnvironmentVariablesConfigurationSource : IConfigurationSource
{
    private readonly string _prefix;

    public SystemEnvironmentVariablesConfigurationSource(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException($"'{nameof(prefix)}' cannot be null or whitespace.", nameof(prefix));
        }

        _prefix = prefix;
    }

    /// <summary>
    /// Builds the <see cref="SystemEnvironmentVariableConfigProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="SystemEnvironmentVariableConfigProvider"/></returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SystemEnvironmentVariableConfigProvider(_prefix);
    }
}
