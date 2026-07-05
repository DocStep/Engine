using System.Runtime.InteropServices;

namespace Engine;


public class ConsoleUtils {


    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole ();
    public static bool usesConsole = false;
    public static void ConsoleStart () {
        usesConsole = true;
        AllocConsole();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        ConsoleUtils.SetConsolePosition(10, 10);
        Console.WriteLine($"Console started");
    }


    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow ();

    [DllImport("user32.dll")]
    static extern bool SetWindowPos (IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOZORDER = 0x0004;

    public static void SetConsolePosition (int x, int y) {
        IntPtr h = GetConsoleWindow();
        SetWindowPos(h, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
    }

}
