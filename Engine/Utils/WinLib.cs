using System.Runtime.InteropServices;

namespace Engine;


public static class WinLib {

    public const int message_status_time = 5000;

    /// Win10 Dark Frame
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute (IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    public static void EnableDarkMode (IntPtr handle) {
        int useDark = 1;
        /// 91, 0, 255
        DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }

    /// Win11 Dark Frame
    const int DWMWA_CAPTION_COLOR = 35;
    public static void SetTitleBarColor (IntPtr handle) {
        int color = 0x000000; /// RGB black (0x00BBGGRR format not needed here)
        DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref color, sizeof(int));
    }


    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme (IntPtr hWnd, string pszSubAppName, string pszSubIdList);

    public static void Apply (IntPtr handle) {
        SetWindowTheme(handle, "DarkMode_Explorer", null);
    }


}
