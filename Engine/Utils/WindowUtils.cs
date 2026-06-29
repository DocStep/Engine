using System.Runtime.InteropServices;
using Silk.NET.Windowing;

public static class WindowUtils {

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute (IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern int SetWindowPos (IntPtr hwnd, IntPtr hwndInsertAfter,
    int x, int y, int cx, int cy, uint flags);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    public static void EnableDarkTitleBar (IWindow window) {
        if (!OperatingSystem.IsWindows())
            return;

        IntPtr hwnd = window.Native?.Win32?.Hwnd ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
            return;

        int useDark = 1;
        int result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        if (result != 0) {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));
        }

        /// Force the non-client area (titlebar) to redraw, since DWM doesn't always repaint on its own
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

}