using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using System;
using WixToolset.Dtf.WindowsInstaller;
using System.Net.Http;
using System.Net;

namespace DirectorySync.Installer.Actions.Actions
{
    /// <summary>
    /// Tries to bind to LDAP server with a specified path and credential.
    /// </summary>
    internal static class PullCloudSettingsAction
    {
        const string _urlIsEmpty = "Multifactor Cloud API url is empty.";
        const string _keyIsEmpty = "Multifactor Cloud API key is empty.";
        const string _secretIsEmpty = "Multifactor Cloud API secret is empty.";

        public static void Execute(Session session, SessionLogger logger)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.ConsumeProperty("PROP_APIURL");
            logger.ConsumeProperty("PROP_APIKEY");
            logger.ConsumeProperty("PROP_APISECRET", true);

            var url = session["PROP_APIURL"];
            var key = session["PROP_APIKEY"];
            var secret = session["PROP_APISECRET"];

            if (string.IsNullOrEmpty(url))
            {
                logger.Log(_urlIsEmpty);
                Notification.Warning($"{_urlIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                logger.Log(_keyIsEmpty);
                Notification.Warning($"{_keyIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }

            if (string.IsNullOrEmpty(secret))
            {
                logger.Log(_secretIsEmpty);
                Notification.Warning($"{_secretIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");
                return;
            }

            try
            {
                using (var cli = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    var api = GetApi(cli, url, key, secret);
                    _ = api.GetConfigAsync().GetAwaiter().GetResult();
                }

                Notification.Success("Multifactor Cloud API integration is OK.");
                return;
            }
            catch (PullCloudConfigException ex)
            {
                logger.LogError(ex);
                logger.Log(ex.Response.ToString());
                Notification.Warning($"Error: {ex.Message}{Environment.NewLine}Log file: {logger.FilePath}");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Notification.Warning($"Error: {ex.Message}{Environment.NewLine}Log file: {logger.FilePath}");
                return;
            }
        }

        private static CloudConfigApi GetApi(HttpClient cli, string url, string key, string secret)
        {
            cli.BaseAddress = new Uri(url);

            var auth = new BasicAuthHeaderValue(key, secret);
            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");
            cli.DefaultRequestHeaders.Add("mf-trace-id", $"ds-installer-{Guid.NewGuid()}");

            return new CloudConfigApi(cli);
        }
    }
}
