using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Engine.Graphics;

namespace Engine;


public class Engine : IDisposable {
    public Engine () {
        Instance = this;
    }

    public static Engine Instance = null!;


    private IWindow Window = null!;
    //private OpenGL.GL GL = null!;
    private Renderer Renderer = null!;
    internal IInputContext Input = null!;
    

    private Camera Camera = null!;
    



    public void Run () {
        var options = WindowOptions.Default with {
            Size = new Vector2D<int>(1280, 720),
            Title = "Survival Engine",
            VSync = false,
        };

        Window = Silk.NET.Windowing.Window.Create(options);

        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(Window);
        var monitor = monitors.First();
        var screenSize = monitor.VideoMode.Resolution ?? new Vector2D<int>(1920, 1080);
        Window.Position = new Vector2D<int>(
            (screenSize.X - options.Size.X)/2,
            (screenSize.Y - options.Size.Y)/2);

        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        Window.Closing += OnClosing;

        Window.Run();
    }

    private void OnLoad () {
        Input = Window.CreateInput();
        foreach (var keyboard in Input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }

        Renderer = new Renderer(Window);

        Camera = new Camera(Window);
        Camera.LookAtOrbitCenter();
        Camera.Update(Window, Input, 0);
    }

    private void OnUpdate (double deltaTime) {
        Camera.Update(Window, Input, deltaTime);
    }

    private void OnKeyDown (IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Window.Close();
        }
    }

    private void OnClosing () {

    }

    public void Dispose () {
        Window?.Dispose();
    }

}