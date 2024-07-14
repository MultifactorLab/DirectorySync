using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectorySync.Application.Measuring
{
    public static class AddCodeTimerExtension
    {
        public static void AddCodeTimer(this HostApplicationBuilder builder, string? configSectionName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<MeasuringOptions>().BindConfiguration(configSectionName ?? String.Empty);
            builder.Services.AddSingleton<CodeTimer>();
        }
    }
}
