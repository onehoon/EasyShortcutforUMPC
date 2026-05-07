using System;
using System.IO;
using System.Diagnostics;

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

        [Conditional("DEBUG")]
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
                // Debug diagnostics are best-effort; app behavior must not depend on log I/O.
            }
        }
    }
}
