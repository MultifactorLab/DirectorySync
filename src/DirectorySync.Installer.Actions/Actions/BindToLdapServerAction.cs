using System;
using WixToolset.Dtf.WindowsInstaller;
using DirectorySync.Infrastructure.Shared.Integrations.Ldap;
using DirectorySync.Infrastructure.Shared.Multifactor.Core.Ldap;
using Microsoft.Extensions.Options;

namespace DirectorySync.Installer.Actions.Actions
{
    internal static class BindToLdapServerAction
    {
        const string _pathIsEmpty = "LDAP server path is empty.";
        const string _usernameIsEmpty = "LDAP server username is empty.";
        const string _passwordIsEmpty = "LDAP server password is empty.";

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

            logger.ConsumeProperty("PROP_LDAP_PATH");
            logger.ConsumeProperty("PROP_LDAP_USERNAME");
            logger.ConsumeProperty("PROP_LDAP_PASSWORD", true);

            var path = session["PROP_LDAP_PATH"];
            var username = session["PROP_LDAP_USERNAME"];
            var password = session["PROP_LDAP_PASSWORD"];

            if (string.IsNullOrEmpty(path))
            {
                logger.Log(_pathIsEmpty);
                Notification.Warning($"{_pathIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                logger.Log(_passwordIsEmpty);
                Notification.Warning($"{_passwordIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                logger.Log(_usernameIsEmpty);
                Notification.Warning($"{_usernameIsEmpty}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }

            try
            {
                var connString = new LdapConnectionString(path);
                var option = Options.Create(new LdapOptions
                {
                    Path = path,
                    Username = username,
                    Password = password,
                    PageSize = 1,
                    Timeout = TimeSpan.FromSeconds(10)
                });
                var factory = new LdapConnectionFactory(connString, option);

                using (var conn = factory.CreateConnection())
                {
                    Notification.Success("LDAP server connection established.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Notification.Warning($"Error: {ex.Message}{Environment.NewLine}Log file: {logger.FilePath}");

                return;
            }
        }
    }
}
