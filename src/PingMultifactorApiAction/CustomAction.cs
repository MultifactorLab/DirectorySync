using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace PingMultifactorApiAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult PingMultifactorApi(Session session)
        {
            session.Log("Begin PingMultifactorApi");

            var url = session["PROP_APIURL"];
            var key = session["PROP_APIKEY"];
            var secret = session["PROP_APISECRET"];

            if (url == null)
            {
                return ActionResult.Failure;
            }

            if (key == null)
            {
                return ActionResult.Failure;
            }

            if (secret == null)
            {
                return ActionResult.Failure;
            }

            var provider = BuildProvider(url, key, secret);

            var api = provider.GetRequiredService<CloudConfigApi>();

            try
            {
                _ = api.GetConfigAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                session.Log(ex.Message);
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        private static ServiceProvider BuildProvider(string url, string key, string secret)
        {
            var services = new ServiceCollection();
            services.AddTransient<CloudConfigApi>();
            services.AddHttpClient("CloudConfigApi", (prov, cli) =>
            {
                cli.BaseAddress = new Uri(url);
                var auth = new BasicAuthHeaderValue(key, secret);
                cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");
            });

            var provider = services.BuildServiceProvider();
            return provider;
        }
    }
}
