namespace Engine;


public static class ArgsUtils {

    private static bool isConsole = true;


    public static void Init () {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Contains("-console")) {
            isConsole = true;
            ConsoleUtils.ConsoleStart();
        }

    }


}
