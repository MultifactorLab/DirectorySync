﻿using DirectorySync.Application.Integrations.Ldap.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DirectorySync.Application.Ports;
using Multifactor.Core.Ldap;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Integrations.Ldap.Windows.Extensions
{
    internal static class AddLdapWindowsIntegrationExtension
    {
        public static void AddLdapWindowsIntegration(this HostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<LdapOptions>()
                .BindConfiguration("Ldap")
                .ValidateDataAnnotations();

            builder.Services.AddTransient(prov =>
            {
                var options = prov.GetRequiredService<IOptions<LdapOptions>>().Value;
                return new LdapConnectionString(options.Path);
            });

            builder.Services.AddTransient<LdapConnectionFactory>();
            builder.Services.AddTransient<BaseDnResolver>();
            builder.Services.AddTransient<IGetReferenceGroup, GetReferenceGroupWithLdapConnection>();
        }
    }
}
