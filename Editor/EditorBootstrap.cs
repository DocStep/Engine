namespace Editor.Graphics;


internal static class EditorBootstrap {

    //[System.Runtime.CompilerServices.ModuleInitializer]
    //internal static void Init () {
    private static void Main (string[] args) {
        Engine.Engine engine = new Engine.Engine(typeof(RendererEditor), () => EditorUI.CreateSingleton());
        engine.Run();
    }

}
