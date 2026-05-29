using System.Windows.Forms;

namespace DirectorySync.Installer.Actions
{
    internal static class Notification
    {
        public static void Success(string message)
        {
            MessageBox.Show(
                text: message,
                caption: "Success",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }

        public static void Warning(string message)
        {
            MessageBox.Show(
                text: message,
                caption: "Warning",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
        }
    }
}
