using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Application.Measuring;

internal static class AddCodeTimerExtension
{
    /// <summary>
    /// Adds some tools for code execution time measuring.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configSectionName"></param>
    public static void AddCodeTimer(this HostApplicationBuilder builder, string? configSectionName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<MeasuringOptions>().BindConfiguration(configSectionName ?? string.Empty);
        builder.Services.AddSingleton<CodeTimer>();
    }
}
