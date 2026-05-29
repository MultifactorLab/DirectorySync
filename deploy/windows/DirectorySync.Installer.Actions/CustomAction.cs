using DirectorySync.Installer.Actions.Actions;
using System;
using WixToolset.Dtf.WindowsInstaller;

namespace DirectorySync.Installer.Actions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult PullCloudSettings(Session session)
        {
            using (var logger = SessionLogger.Create(session, nameof(PullCloudSettings)))
            {
                try
                {
                    PullCloudSettingsAction.Execute(session, logger);
                }
                catch (Exception ex)
                {
                    Notification.Warning($"Error: {ex.Message}{Environment.NewLine}Log file: {logger.FilePath}");
                    logger.LogError(ex.ToString());
                }

                return ActionResult.Success;
            }
        }

        [CustomAction]
        public static ActionResult BindToLdapServer(Session session)
        {      
            using (var logger = SessionLogger.Create(session, nameof(BindToLdapServer)))
            {
                try
                {
                    BindToLdapServerAction.Execute(session, logger);
                }
                catch (Exception ex)
                {
                    Notification.Warning($"Error: {ex.Message}{Environment.NewLine}Log file: {logger.FilePath}");
                    logger.LogError(ex.ToString());
                }
                return ActionResult.Success;
            }
        }
    }
}
