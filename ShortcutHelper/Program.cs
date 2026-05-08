using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
    private const long MaxLogBytes = 64 * 1024;
    private const long KeepLogBytes = 32 * 1024;
    private const int DuplicateGuardMs = 700;
    // Small settle delay after Game Bar button activation before attempting focus/input handoff.
    private const int InitialInputSettleDelayMs = 120;
    // Additional wait after focus returns, to avoid key delivery racing with overlay teardown.
    private const int PostFocusSettleDelayMs = 420;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static string GuardPath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyShortcutForUMPC");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "helper.guard");
        }
    }

    private static string LogPath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyShortcutForUMPC");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "helper.log");
        }
    }

    private static void Main(string[] args)
    {
        try
        {
            Log($"start args={string.Join(",", args ?? Array.Empty<string>())}");
            LogForeground("before");

            if (args == null || args.Length == 0)
            {
                Log("no args; exit");
                return;
            }

            // Game Bar button click can momentarily keep overlay/input focus; wait briefly before handoff.
            Thread.Sleep(InitialInputSettleDelayMs);
            var action = ResolveAction(args);
            Log($"resolved action={action}");
            if (string.IsNullOrEmpty(action))
            {
                Log("no supported action resolved; exit");
                return;
            }

            if (ShouldSkipDuplicate(action))
            {
                Log($"duplicate guard skip action={action}");
                return;
            }

            CloseGameBarAndWaitForFocusReturn();
            var ok = action switch
            {
                "insert" => PressKey(0x2D, isExtended: true),
                "altinsert" => PressCombo(new (ushort vk, bool ext)[] { (0x12, false), (0x2D, true) }),
                "home" => PressKey(0x24, isExtended: true),
                "end" => PressKey(0x23, isExtended: true),
                "losslessscaling" => PressCombo(new (ushort vk, bool ext)[] { (0x11, false), (0x12, false), (0x53, false) }),
                "quit" => PressCombo(new (ushort vk, bool ext)[] { (0x12, false), (0x73, false) }),
                _ => false
            };

            Log($"action={action} result={ok}");
            LogForeground("after");
        }
        catch (Exception ex)
        {
            Log($"exception {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string ResolveAction(string[] args)
    {
        foreach (var raw in args)
        {
            var arg = (raw ?? string.Empty).Trim().ToLowerInvariant();
            switch (arg)
            {
                case "insert":
                case "altinsert":
                case "home":
                case "end":
                case "capture":
                case "losslessscaling":
                case "quit":
                    return arg;
            }
        }

        return string.Empty;
    }

    private static bool ShouldSkipDuplicate(string action)
    {
        try
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var path = GuardPath;

            if (File.Exists(path))
            {
                var raw = File.ReadAllText(path);
                var parts = raw.Split('|');
                if (parts.Length == 2 &&
                    long.TryParse(parts[0], out var prevTicks) &&
                    string.Equals(parts[1], action, StringComparison.OrdinalIgnoreCase))
                {
                    var elapsedMs = TimeSpan.FromTicks(nowTicks - prevTicks).TotalMilliseconds;
                    if (elapsedMs >= 0 && elapsedMs < DuplicateGuardMs)
                    {
                        return true;
                    }
                }
            }

            File.WriteAllText(path, $"{nowTicks}|{action}");
        }
        catch
        {
            // If guard state fails, continue normal execution to avoid blocking user input.
        }

        return false;
    }

    private static void CloseGameBarAndWaitForFocusReturn()
    {
        Log("close gamebar requested");
        PressCombo(new (ushort vk, bool ext)[] { (0x5B, false), (0x47, false) });

        var nonGameBarStreak = 0;
        for (var attempt = 1; attempt <= 8; attempt++)
        {
            Thread.Sleep(attempt == 1 ? 260 : 160);
            var procName = GetForegroundProcessName();
            Log($"focus poll attempt={attempt} proc={procName}");
            if (!IsGameBarProcess(procName))
            {
                nonGameBarStreak++;
                if (nonGameBarStreak >= 2)
                {
                    Log($"focus returned to proc={procName}");
                    Thread.Sleep(PostFocusSettleDelayMs);
                    return;
                }
            }
            else
            {
                nonGameBarStreak = 0;
            }
        }

        Thread.Sleep(PostFocusSettleDelayMs);
        LogForeground("after-close-timeout");
    }

    private static bool IsGameBarProcess(string procName)
    {
        return string.Equals(procName, "GameBar", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(procName, "GameBarFTServer", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(procName, "XboxPcAppFT", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return "unknown";
        }

        try
        {
            return Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }

    private static bool PressCombo((ushort vk, bool ext)[] keys)
    {
        try
        {
            foreach (var k in keys)
            {
                PressVirtualKey(k.vk, false, k.ext);
            }
            for (var i = keys.Length - 1; i >= 0; i--)
            {
                PressVirtualKey(keys[i].vk, true, keys[i].ext);
            }

            Log($"send combo legacy count={keys.Length * 2}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"send combo legacy exception={ex.GetType().Name}:{ex.Message}");
            return false;
        }
    }

    private static bool PressKey(ushort vk, bool isExtended)
    {
        try
        {
            PressVirtualKey(vk, false, isExtended);
            PressVirtualKey(vk, true, isExtended);
            Log($"send key legacy vk=0x{vk:X2}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"send key legacy exception={ex.GetType().Name}:{ex.Message}");
            return false;
        }
    }

    private static void PressVirtualKey(ushort vk, bool keyUp, bool isExtended)
    {
        uint flags = 0;
        if (keyUp) flags |= KEYEVENTF_KEYUP;
        if (isExtended) flags |= KEYEVENTF_EXTENDEDKEY;
        keybd_event((byte)vk, 0, flags, UIntPtr.Zero);
    }

    [Conditional("DEBUG")]
    private static void LogForeground(string tag)
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        string procName = "unknown";
        try
        {
            if (pid != 0)
            {
                procName = Process.GetProcessById((int)pid).ProcessName;
            }
        }
        catch
        {
            // Best-effort debug logging only; process lookup failures should not affect helper execution.
        }
        Log($"{tag} fg hwnd=0x{hwnd.ToInt64():X} pid={pid} proc={procName}");
    }

    [Conditional("DEBUG")]
    private static void Log(string message)
    {
        try
        {
            TrimLogIfNeeded();
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // Debug log write failure is non-fatal; never block shortcut execution.
        }
    }

    [Conditional("DEBUG")]
    private static void TrimLogIfNeeded()
    {
        try
        {
            var path = LogPath;
            if (!File.Exists(path))
            {
                return;
            }

            var info = new FileInfo(path);
            if (info.Length <= MaxLogBytes)
            {
                return;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var keep = (int)Math.Min(KeepLogBytes, info.Length);
            fs.Seek(-keep, SeekOrigin.End);
            var buffer = new byte[keep];
            var read = fs.Read(buffer, 0, keep);
            File.WriteAllBytes(path, buffer.AsSpan(0, read).ToArray());
        }
        catch
        {
            // If trimming fails, keep going and let next debug writes continue best-effort.
        }
    }
}



