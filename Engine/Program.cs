namespace Engine;

internal static class Program {
    private static void Main (string[] args) {
        //using var game = new MonoGame.Game();
        using var game = new SilkNet.Game();
        game.Run();
    }
}
