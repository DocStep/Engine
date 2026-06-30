using System.Runtime.InteropServices;

namespace Engine;


public static class ArgsUtils {

    private static bool isConsole = true;


    public static void Args () {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Contains("-console")) {
            isConsole = true;
            ConsoleUtils.ConsoleStart();
        }

    }


}
