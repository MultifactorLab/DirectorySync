using DirectorySync.Installer.Actions.Actions;
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
                PullCloudSettingsAction.Execute(session, logger);
                return ActionResult.Success;
            }
        }

        [CustomAction]
        public static ActionResult BindToLdapServer(Session session)
        {
            using (var logger = SessionLogger.Create(session, nameof(BindToLdapServer)))
            {
                BindToLdapServerAction.Execute(session, logger);
                return ActionResult.Success;
            }
        }
    }
}
