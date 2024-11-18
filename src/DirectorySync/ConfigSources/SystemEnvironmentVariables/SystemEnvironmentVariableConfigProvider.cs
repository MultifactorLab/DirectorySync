using System.Collections;

namespace DirectorySync.ConfigSources.SystemEnvironmentVariables;

internal class SystemEnvironmentVariableConfigProvider : ConfigurationProvider
{
    private readonly string _prefix;
    private readonly string _normalizedPrefix;

    /// <summary>
    /// Initializes a new instance with the specified prefix.
    /// </summary>
    /// <param name="prefix">A prefix used to filter the environment variables.</param>
    public SystemEnvironmentVariableConfigProvider(string? prefix)
    {
        _prefix = prefix ?? string.Empty;
        _normalizedPrefix = Normalize(_prefix);
    }

    /// <summary>
    /// Loads the Machine-level environment variables.
    /// </summary>
    public override void Load() => Load(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));

    /// <summary>
    /// Generates a string representing this provider name and relevant details.
    /// </summary>
    /// <returns> The configuration name. </returns>
    public override string ToString()
    {
        string s = GetType().Name;
        if (!string.IsNullOrEmpty(_prefix))
        {
            s += $" Prefix: '{_prefix}'";
        }
        return s;
    }

    internal void Load(IDictionary envVariables)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        IDictionaryEnumerator e = envVariables.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                string key = (string)e.Entry.Key;
                string? value = (string?)e.Entry.Value;

                AddIfNormalizedKeyMatchesPrefix(data, Normalize(key), value);
            }
        }
        finally
        {
            (e as IDisposable)?.Dispose();
        }

        Data = data;
    }

    private void AddIfNormalizedKeyMatchesPrefix(Dictionary<string, string?> data, string normalizedKey, string? value)
    {
        if (normalizedKey.StartsWith(_normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            data[normalizedKey.Substring(_normalizedPrefix.Length)] = value;
        }
    }

    private static string Normalize(string key) => key.Replace("__", ConfigurationPath.KeyDelimiter);
}
