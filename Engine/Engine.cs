using Silk.NET.Windowing;
using Engine.Graphics;
using Engine.Input;

namespace Engine;


public class Engine : IDisposable {
    public Engine () {
        Instance = this;
    }

    public static Engine Instance = null!;

    public Action? de_Update = null;
    public Action? de_FixedUpdate = null;
    public static string savesFolder = "Data";

    internal static IWindow Window = null!;
    //internal static IKeyboard? keyboard;
    //internal static IMouse? mouse;

    private Renderer Renderer = null!;
    //internal IInputContext Input = null!;
    private Camera Camera = null!;

    private double _deltaTime;
    public static double deltaTime {
        get => Instance._deltaTime;
        private set => Instance._deltaTime = value;
    }

    /// Debug
    public EngineStates engineState = EngineStates.Loading;
    public bool debug = false;
    public double sessionTime = 0f;
    
    public float deltaTimeCalculated;



    public void Run () {
        ConsoleUtils.SetConsolePosition(10, 10);
        Console.WriteLine($"========== Init ==========");

        ThreadUtils.Init();
        Log.InstanceNew();
        TimeUtils.Init();
        Log.log($"Time: {TimeUtils.getCurrentTime}");
        Reflection.InstanceNew();
        Json.InstanceNew();

        Inputs.AddActionsFirst(DataEngine.InputsData);

        Inputs.KeysInit();
        Console.WriteLine($"========== Init Finish ==========");

        var options = WindowOptions.Default with {
            Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
            Title = "Engine",
            VSync = false,
        };

        Window = Silk.NET.Windowing.Window.Create(options);

        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(Window);
        var monitor = monitors.First();
        var screenSize = monitor.VideoMode.Resolution ?? new Silk.NET.Maths.Vector2D<int>(1920, 1080);
        Window.Position = new Silk.NET.Maths.Vector2D<int>(
            (screenSize.X - options.Size.X)/2,
            (screenSize.Y - options.Size.Y)/2);

        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        //Window.Closing += OnClosing;

        Window.Run();
    }

    private void OnLoad () {
        Silk.NET.Input.IInputContext Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Window);
        InputState.Init(Input);

        /*foreach (var keyboard in Input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }*/

        Renderer = new Renderer();

        SceneManager.Init();

        Camera = new CameraEditor();
    }

    private void OnUpdate (double deltaTime) {
        _deltaTime = deltaTime;
        Update();

        if (Inputs.Actions[Inputs.NavBack].pressed) Window.Close();
    }

    void FixedUpdate () {
        DataEngine.global_audio_Mult = 1f;
        //Debug.Log(de_FixedUpdate?.GetInvocationList().Length ?? 0);
        de_FixedUpdate?.Invoke();
    }
    void Update () {
        InputState.Update();
        Inputs.Update();

        de_Update?.Invoke();

        /// Counters
        sessionTime += _deltaTime;
        //deltaTimeCalculated += (Time.unscaledDeltaTime - deltaTimeCalculated)*0.1f;
    }



    /*private void OnKeyDown (IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Window.Close();
        }
    }*/

    private void OnClosing () {

    }

    public void Dispose () {
        Window?.Dispose();
    }

}