namespace Engine;


public sealed class EngineLaunchOptions {
    private EngineLaunchOptions (string[] arguments, bool editor, string? scenePath) {
        Arguments = arguments;
        IsEditor = editor;
        ScenePath = scenePath;
    }

    public string[] Arguments { get; }
    public bool IsEditor { get; }
    public string? ScenePath { get; }

    public static EngineLaunchOptions Parse (string[] arguments) {
        bool editor = false;
        string? scenePath = null;

        for (int i = 0; i < arguments.Length; i++) {
            string argument = arguments[i];
            if (argument is "-editor" or "--editor") {
                editor = true;
            } else if (argument is "-scene" or "-scene") {
                if (i + 1 >= arguments.Length || arguments[i + 1].StartsWith('-'))
                    throw new ArgumentException($"{argument} requires a scene name or path.");
                scenePath = arguments[++i];
            } else if (argument.StartsWith("-scene=", StringComparison.Ordinal)) {
                scenePath = argument["-scene=".Length..];
                if (string.IsNullOrWhiteSpace(scenePath))
                    throw new ArgumentException("-scene requires a scene name or path.");
            }
        }

        return new EngineLaunchOptions(arguments, editor, scenePath);
    }
}
