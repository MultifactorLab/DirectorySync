using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Infrastructure.Adapters.Ldap;
using DirectorySync.Infrastructure.Adapters.Ldap.Helpers;
using DirectorySync.Infrastructure.Adapters.Ldap.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Extensions;

public static class DirectoryAdapterBuilderExtensions
{
    public static void AddLdapAdapter(this HostApplicationBuilder builder, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<LdapOptions>()
            .BindConfiguration("Ldap")
            .ValidateDataAnnotations();            
            
        builder.Services.AddOptions<LdapRequestOptions>()
            .BindConfiguration("Sync")
            .ValidateDataAnnotations();
        
        builder.Services.AddOptions<LdapAttributeMappingOptions>()
            .BindConfiguration("Sync:PropertyMapping")
        .ValidateDataAnnotations();

        builder.Services.AddSingleton(prov =>
        {
            return LdapConnectionFactory.Create();
        });

        builder.Services.AddSingleton(prov =>
        {
            var factory = prov.GetRequiredService<LdapConnectionFactory>();

            return new LdapSchemaLoader(factory);
        });

        builder.Services.AddSingleton<BaseDnResolver>();
        builder.Services.AddSingleton<LdapDomainDiscovery>();
        builder.Services.AddSingleton<LdapFinder>();

        builder.Services.AddTransient<ILdapGroupPort, LdapGroup>();
        builder.Services.AddTransient<ILdapMemberPort, LdapMember>();
    }
}
