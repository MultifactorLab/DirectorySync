using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WixToolset.Dtf.WindowsInstaller;

namespace DirectorySync.Installer.Actions
{
    internal class SessionProperty
    {
        public string Name { get; }
        public bool Secure { get; }

        public SessionProperty(string name, bool secure = false)
        {
            Name = name;
            Secure = secure;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    internal class SessionLogger : IDisposable
    {
        private readonly Session _session;
        private readonly string _category;
        private readonly StringBuilder _sb;
        private readonly Dictionary<string, SessionProperty> _properties;

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

        private SessionLogger(Session session, string category)
        {
            _session = session;
            _category = category;
            _sb = new StringBuilder();
            _properties = new Dictionary<string, SessionProperty>();
        }

        /// <summary>
        /// Creates a new instance of Session logger.
        /// </summary>
        /// <param name="session">Installer session.</param>
        /// <returns>Session logger.</returns>
        public static SessionLogger Create(Session session, string category)
        {
            return new SessionLogger(session, category ?? string.Empty);
        }

        public void Log(string message)
        {
            _sb.AppendLine($"[{DateTime.Now:O} INF] [{_category}]: {message}");
        }

        public void LogError(string message)
        {
            _sb.AppendLine($"[{DateTime.Now:O} ERR] [{_category}]: {message}");
        }        
        
        public void LogError(Exception ex)
        {
            _sb.AppendLine($"[{DateTime.Now:O} ERR] [{_category}]: {ex}");
        }

        /// <summary>
        /// Before the logger will been disposed the specified property will be consumed from the <see cref="Session"/>.
        /// </summary>
        /// <param name="sessionPropertyName">Property name.</param>
        /// <param name="secure">Property value should be masked.</param>
        /// <returns>Current instance of SessionLogger</returns>
        public SessionLogger ConsumeProperty(string sessionPropertyName, bool secure = false)
        {
            _properties[sessionPropertyName] = new SessionProperty(sessionPropertyName, secure);
            return this;
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
            finally
            {
                _sb.Clear();
            }
        }

        private void LogSessionData()
        {
            if (_properties.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder("Session Properties");
            sb.AppendLine();
            foreach (var prop in _properties.Values)
            {
                var value = _session[prop.Name];
                var sanitized = prop.Secure ? MaskIfNotEmpty(value) : value;
                sb.AppendFormat("   {0}: {1}{2}", prop.Name, sanitized, Environment.NewLine);
            }

            Log(sb.ToString());
        }

        private static string MaskIfNotEmpty(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return "***";
        }

        private void ShowAlert()
        {
            Notification.Warning($"Failed to write log to file. And now we will just show you this log: {Environment.NewLine}{_sb}");
        }

        public void Dispose()
        {
            LogSessionData();
            Flush();
        }
    }
}
