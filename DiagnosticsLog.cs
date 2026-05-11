using System;
using System.IO;
using Windows.Storage;

namespace Quick_Buttons_for_Game_Bar
{
    internal static class DiagnosticsLog
    {
        private static readonly object Sync = new object();
        private static string _cachedLogPath;

        private static string LogPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachedLogPath))
                {
                    return _cachedLogPath;
                }

                string dir = null;
                try
                {
                    dir = ApplicationData.Current.LocalFolder.Path;
                }
                catch
                {
                    // Fall through to temp path when LocalFolder is unavailable.
                }

                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = Path.Combine(Path.GetTempPath(), "QuickButtonsForGameBar");
                }

                Directory.CreateDirectory(dir);
                _cachedLogPath = Path.Combine(dir, "diagnostics.log");
                return _cachedLogPath;
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
                // Debug diagnostics are best-effort; app behavior must not depend on log I/O.
            }
        }

        internal static void WriteException(string context, Exception ex)
        {
            if (ex == null)
            {
                Write($"{context} exception=(null)");
                return;
            }

            Write($"{context} ex={ex.GetType().FullName} hr=0x{ex.HResult:X8} msg={ex.Message} stack={ex.StackTrace}");
        }
    }
}
