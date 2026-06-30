namespace Game;


internal static class Program {
    private static void Main (string[] args) {
        //using var game = new MonoGame.Game();
        //game.Run();
        Engine.Engine.Instance.Run();
    }

}
