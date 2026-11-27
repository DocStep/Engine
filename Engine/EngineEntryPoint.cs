namespace Engine;


public sealed class EngineEntryPoint {

    public string[]? Arguments = null;
    public Type? Renderer = null;
    public Action? OnLoad = null;

    /// Editor-specific hooks are used only when the engine is launched with -editor.
    public Type? EditorRenderer = null;
    public Action? OnEditorLoad = null;
    public bool Editor = false;

}
