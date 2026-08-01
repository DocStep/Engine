namespace Engine;


public static class ArgsUtils {

    public static bool isConsole { get; private set; } = true;
    public static bool isEditor = false;


    public static void Init () {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Contains("-console")) {
            isConsole = true;
            ConsoleUtils.ConsoleStart();
        }

        if (args.Contains("-editor")) {
            isEditor = true;
        }

    }


}
