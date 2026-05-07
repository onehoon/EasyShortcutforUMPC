using System;
using System.IO;

namespace Easy_Shortcut_for_UMPC
{
    internal static class DiagnosticsLog
    {
        private static readonly object Sync = new object();

        private static string LogPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EasyShortcutForUMPC");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "uwp.log");
            }
        }

        internal static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(
                        LogPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
                }
            }
            catch
            {
            }
        }
    }
}
