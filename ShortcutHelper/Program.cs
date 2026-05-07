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
            var action = args[0].ToLowerInvariant();
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

    private static bool PressCombo((ushort vk, bool ext)[] keys)
    {
        var inputs = new INPUT[keys.Length * 2];
        var idx = 0;

        foreach (var k in keys)
        {
            inputs[idx++] = BuildKeyInput(k.vk, false, k.ext);
        }
        for (var i = keys.Length - 1; i >= 0; i--)
        {
            inputs[idx++] = BuildKeyInput(keys[i].vk, true, keys[i].ext);
        }

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        var err = Marshal.GetLastWin32Error();
        Log($"send combo inputs={inputs.Length} sent={sent} err={err}");
        return sent == inputs.Length;
    }

    private static bool PressKey(ushort vk, bool isExtended)
    {
        var inputs = new[]
        {
            BuildKeyInput(vk, false, isExtended),
            BuildKeyInput(vk, true, isExtended),
        };
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        var err = Marshal.GetLastWin32Error();
        Log($"send key vk=0x{vk:X2} inputs=2 sent={sent} err={err}");
        return sent == 2;
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
