using System;
using System.Windows.Forms;
using System.IO;
using System.Text;
using WixToolset.Dtf.WindowsInstaller;

namespace DirectorySync.Installer.Actions
{
    internal class SessionLogger : IDisposable
    {
        private readonly Session _session;
        private readonly StringBuilder _sb;

        private readonly Lazy<string> _path = new Lazy<string>(() =>
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var path = Path.Combine(dir, "mf-directorysync-install.log");

                return path;
            }
            catch
            {
                return string.Empty;
            }
        });


        public string FilePath => _path.Value;

        private SessionLogger(Session session)
        {
            _session = session;
            _sb = new StringBuilder();

            Log("Session Components:");
            foreach (var comp in session.Components)
            {
                Log(comp.Name);
            }
        }

        public static SessionLogger Create(Session session)
        {
            return new SessionLogger(session);
        }

        public void Log(string message)
        {
            _sb.AppendLine($"[{DateTime.Now:O} INF]: {message}");
        }

        public void LogError(string message)
        {
            _sb.AppendLine($"[{DateTime.Now:O} ERR]: {message}");
        }        
        
        public void LogError(Exception ex)
        {
            _sb.AppendLine($"[{DateTime.Now:O} ERR]: {ex}");
        }

        private void Flush()
        {
            if (FilePath == string.Empty)
            {
                ShowAlert();
                return;
            }

            try
            {
                File.AppendAllText(FilePath, _sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                ShowAlert();
            }
        }

        private void LogSessionProperties()
        {
            Log("Session Properties:");
            Log($"PROP_APIURL: {_session["PROP_APIURL"]}");
            Log($"PROP_APIKEY: {_session["PROP_APIKEY"]}");
            Log($"PROP_APISECRET: {MaskIfNotEmpty(_session["PROP_APISECRET"])}");
        }
        private static string MaskIfNotEmpty(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return $"{str[0]}***";
        }

        private void ShowAlert()
        {
            MessageBox.Show(
                text: $"Failed to write log to file. And now we will just show you this log: {Environment.NewLine}{_sb}",
                caption: "Fail",
                buttons: MessageBoxButtons.OK);
        }

        public void Dispose()
        {
            LogSessionProperties();
            Flush();
            _sb.Clear();
        }
    }
}
