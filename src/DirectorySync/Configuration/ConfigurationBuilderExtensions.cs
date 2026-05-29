using DirectorySync.Infrastructure.ConfigurationSources.Cloud;
using DirectorySync.Infrastructure.ConfigurationSources.SystemEnvironmentVariables;
using System.Collections;
using Microsoft.Extensions.Configuration;

namespace DirectorySync.Configuration;

internal static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from SYSTEM environment variables
    /// with a specified prefix.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="prefix">The prefix that SYSTEM (Machine level) environment variable names must start with. The prefix will be removed from the SYSTEM environment variable names.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static void AddSystemEnvironmentVariablesSource(
        this IConfigurationBuilder configurationBuilder,
        string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException($"'{nameof(prefix)}' cannot be null or whitespace.", nameof(prefix));
        }

        configurationBuilder.Add(new SystemEnvironmentVariablesConfigurationSource(prefix));
    }
    
    public static void AddCloudConfigurationSource(this IConfigurationBuilder configurationBuilder)
    {
        var source = new CloudConfigurationSource();
        configurationBuilder.Add(source);
    }

    /// <summary>
    /// Adds process environment variables with the given prefix, but ignores null/empty/whitespace values.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="prefix">Environment variable prefix.</param>
    public static void AddEnvironmentVariablesNonEmpty(
        this IConfigurationBuilder configurationBuilder,
        string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException($"'{nameof(prefix)}' cannot be null or whitespace.", nameof(prefix));
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is not string rawKey || !rawKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = entry.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var key = rawKey[prefix.Length..].Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            data[key] = value;
        }

        configurationBuilder.AddInMemoryCollection(data);
    }
}
