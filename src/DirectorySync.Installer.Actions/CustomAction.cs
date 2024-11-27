using DirectorySync.Infrastructure.Shared.Http;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig;
using Microsoft.Extensions.DependencyInjection;
using System;
using WixToolset.Dtf.WindowsInstaller;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;

namespace DirectorySync.Installer.Actions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult PullCloudSettings(Session session)
        {
            using (var logger = SessionLogger.Create(session))
            {
                logger.Log("Begin CustomActions.PullCloudSettings");

                var url = session["PROP_APIURL"];
                var key = session["PROP_APIKEY"];
                var secret = session["PROP_APISECRET"];

                if (string.IsNullOrEmpty(url))
                {
                    logger.Log("Multifactor Cloud API url is empty");
                    ShowFail("Multifactor Cloud API url is empty");
                    return ActionResult.Success;
                }

                if (string.IsNullOrEmpty(key))
                {
                    logger.Log("Multifactor Cloud API key is empty");
                    ShowFail("Multifactor Cloud API key is empty");
                    return ActionResult.Success;
                }

                if (string.IsNullOrEmpty(secret))
                {
                    logger.Log("Multifactor Cloud API secret is empty");
                    ShowFail("Multifactor Cloud API secret is empty");
                    return ActionResult.Success;
                }

                try
                {
                    var api = GetApi(url, key, secret);
                    _ = api.GetConfigAsync().GetAwaiter().GetResult();

                    ShowOk();
                    return ActionResult.Success;
                }
                catch (PullCloudConfigException ex)
                {
                    logger.LogError(ex);
                    logger.Log(ex.Response.ToString());
                    ShowFail(ex);
                    return ActionResult.Success;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    ShowFail(ex);
                    return ActionResult.Success;
                }
            }
        }

        private static void ShowOk()
        {
            MessageBox.Show(
                text: "Multifactor Cloud API settings are correct", 
                caption: "Success", 
                buttons: MessageBoxButtons.OK);
        }

        private static void ShowFail(string message)
        {
            MessageBox.Show(
                text: message,
                caption: "Fail", 
                buttons: MessageBoxButtons.OK);
        }        
        
        private static void ShowFail(Exception ex)
        {
            MessageBox.Show(
                text: $"Error: {ex.Message}",
                caption: "Fail",
                buttons: MessageBoxButtons.OK);
        }

        private static CloudConfigApi GetApi(string url, string key, string secret)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var cli = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            var auth = new BasicAuthHeaderValue(key, secret);
            cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

            return new CloudConfigApi(cli);
        }
    }
}
