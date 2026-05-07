using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    private const int KEYEVENTF_KEYUP = 0x0002;

    private static void Main(string[] args)
    {
        if (args.Length == 0) return;

        Thread.Sleep(120);

        switch (args[0].ToLowerInvariant())
        {
            case "insert":
                PressKey(0x2D);
                break;
            case "home":
                PressKey(0x24);
                break;
            case "end":
                PressKey(0x23);
                break;
            case "capture":
                PressCombo(0x11, 0x12, 0x53); // Ctrl+Alt+S
                break;
            case "quit":
                PressCombo(0x12, 0x73); // Alt+F4
                break;
        }
    }

    private static void PressCombo(params byte[] keys)
    {
        foreach (var k in keys) keybd_event(k, 0, 0, 0);
        for (var i = keys.Length - 1; i >= 0; i--) keybd_event(keys[i], 0, KEYEVENTF_KEYUP, 0);
    }

    private static void PressKey(byte vk)
    {
        keybd_event(vk, 0, 0, 0);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
    }
}
