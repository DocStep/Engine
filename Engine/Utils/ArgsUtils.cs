namespace Engine;


public static class ArgsUtils {

    public static bool isConsole { get; private set; } = true;
    public static bool isEditor { get; set; } = false;


    public static void Init (string[]? args = null) {
        if (args is null) {
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            args = commandLineArguments.Length > 1 ? commandLineArguments[1..] : Array.Empty<string>();
        }

        if (args.Contains("-console")) {
            isConsole = true;
            ConsoleUtils.ConsoleStart();
        }

        if (args.Contains("-editor")) {
            isEditor = true;
        }

    }


}
