using Game.Samples;

namespace Game;


internal static class Program {
    private static void Main (string[] args) {
        //using MonoGame.Game game = new MonoGame.Game();
        //game.Run();
        Engine.Engine engine = new Engine.Engine(de_Init: () => new SceneMaterials() { Name = "Scene Materials", });
        engine.Run();
    }
}
