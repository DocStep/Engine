namespace Game;

internal static class Program {
    private static void Main (string[] args) {
        //using var game = new MonoGame.Game();
        using var game = new Engine.Engine();
        game.Run();
    }
}
