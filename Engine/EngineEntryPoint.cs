namespace Engine;


public sealed class EngineEntryPoint {

    public Type? Renderer = null;
    public Action? OnLoad = null;

    /// Editor host. The player build leaves this false and never loads the editor assembly.
    public bool Editor = false;

}
