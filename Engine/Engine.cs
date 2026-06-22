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


    internal static IWindow Window = null!;
    internal static IKeyboard? keyboard;
    internal static IMouse? mouse;

    private Renderer Renderer = null!;
    internal IInputContext Input = null!;
    private Camera Camera = null!;

    private double _deltaTime;
    public static double deltaTime {
        get => Instance._deltaTime;
        private set => Instance._deltaTime = value;
    }


    public void Run () {
        var options = WindowOptions.Default with {
            Size = new Vector2D<int>(1280, 720),
            Title = "Engine",
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
        Window.Update += OnUpdate_;
        Window.Closing += OnClosing;

        Window.Run();
    }

    private void OnLoad () {
        Input = Window.CreateInput();
        foreach (var keyboard in Input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }

        Renderer = new Renderer();

        Camera = new CameraEditor();
    }

    private void OnUpdate_ (double deltaTime) {
        Engine.deltaTime = deltaTime;
        keyboard = Engine.Instance.Input.Keyboards.FirstOrDefault();
        mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        if (keyboard.IsKeyPressed(Key.Escape)) Window.Close();
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