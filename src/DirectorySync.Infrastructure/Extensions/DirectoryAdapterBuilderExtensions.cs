using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap;
using DirectorySync.Infrastructure.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Multifactor.Core.Ldap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Extensions;

public static class DirectoryAdapterBuilderExtensions
{
    public static void AddLdapAdapter(this HostApplicationBuilder builder, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<LdapOptions>()
            .BindConfiguration("Ldap")
            .ValidateDataAnnotations();            
            
        builder.Services.AddOptions<RequestOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();

        builder.Services.AddTransient(prov =>
        {
            var options = prov.GetRequiredService<IOptions<LdapOptions>>().Value;
            return new LdapConnectionString(options.Path);
        });

        builder.Services.AddTransient<LdapConnectionFactory>();
        builder.Services.AddTransient<BaseDnResolver>();
        builder.Services.AddTransient<ILdapGroupPort, LdapGroup>();
        builder.Services.AddTransient<ILdapMemberPort, LdapMember>();
    }
}
