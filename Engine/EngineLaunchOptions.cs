namespace Engine;


public sealed class EngineLaunchOptions {
    private EngineLaunchOptions (string[] arguments, bool editor, string? scenePath, string? projectRoot) {
        Arguments = arguments;
        IsEditor = editor;
        ScenePath = scenePath;
        ProjectRoot = projectRoot;
    }

    public string[] Arguments { get; }
    public bool IsEditor { get; }
    public string? ScenePath { get; }
    public string? ProjectRoot { get; }

    public static EngineLaunchOptions Parse (string[] arguments) {
        bool editor = false;
        string? scenePath = null;
        string? projectRoot = null;

        for (int i = 0; i < arguments.Length; i++) {
            string argument = arguments[i];
            if (argument == "-editor") {
                editor = true;
            } else if (argument == "-scene") {
                if (i + 1 >= arguments.Length || arguments[i + 1].StartsWith('-'))
                    throw new ArgumentException($"{argument} requires a scene name or path.");
                scenePath = arguments[++i];
            } else if (argument.StartsWith("-scene=", StringComparison.Ordinal)) {
                scenePath = argument["-scene=".Length..];
                if (string.IsNullOrWhiteSpace(scenePath))
                    throw new ArgumentException("-scene requires a scene name or path.");
            } else if (argument == "-project") {
                if (i + 1 >= arguments.Length || arguments[i + 1].StartsWith('-'))
                    throw new ArgumentException("-project requires a project directory.");
                projectRoot = arguments[++i];
            } else if (argument.StartsWith("-project=", StringComparison.Ordinal)) {
                projectRoot = argument["-project=".Length..];
                if (string.IsNullOrWhiteSpace(projectRoot))
                    throw new ArgumentException("-project requires a project directory.");
            }
        }

        return new EngineLaunchOptions(arguments, editor, scenePath, projectRoot);
    }
}
