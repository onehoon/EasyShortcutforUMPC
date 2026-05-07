using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
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
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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

            Thread.Sleep(120);
            var action = ResolveAction(args);
            Log($"resolved action={action}");
            if (string.IsNullOrEmpty(action))
            {
                Log("no supported action resolved; exit");
                return;
            }
            CloseGameBarAndWaitForFocusReturn();
            var ok = action switch
            {
                "insert" => PressKey(0x2D, isExtended: true),
                "home" => PressKey(0x24, isExtended: true),
                "end" => PressKey(0x23, isExtended: true),
                "capture" => PressCombo(new (ushort vk, bool ext)[] { (0x11, false), (0x12, false), (0x53, false) }),
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
                case "home":
                case "end":
                case "capture":
                case "quit":
                    return arg;
            }
        }

        return string.Empty;
    }

    private static void CloseGameBarAndWaitForFocusReturn()
    {
        Log("close gamebar requested");
        PressCombo(new (ushort vk, bool ext)[] { (0x5B, false), (0x47, false) });

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            Thread.Sleep(attempt == 1 ? 220 : 120);
            var procName = GetForegroundProcessName();
            Log($"focus poll attempt={attempt} proc={procName}");
            if (!IsGameBarProcess(procName))
            {
                Log($"focus returned to proc={procName}");
                return;
            }
        }

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

    private static INPUT BuildKeyInput(ushort vk, bool keyUp, bool isExtended)
    {
        uint flags = 0;
        if (keyUp) flags |= KEYEVENTF_KEYUP;
        if (isExtended) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

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
        catch { }
        Log($"{tag} fg hwnd=0x{hwnd.ToInt64():X} pid={pid} proc={procName}");
    }

    private static void Log(string message)
    {
        File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
    }
}
