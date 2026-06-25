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

    /// Debug
    public EngineStates engineState = EngineStates.Loading;
    public bool debug = false;
    public double sessionTime = 0f;

    public float deltaTimeCalculated;

    private double _time = 0d;
    public static double time {
        get => Instance._time;
        private set => Instance._time = value;
    }
    private double _accumulator = 0d;

    private double _deltaTime;
    public static double deltaTime {
        get => Instance._deltaTime;
        private set => Instance._deltaTime = value;
    }

    private double _fixedDeltaTime = 1d/50d;
    public static double fixedDeltaTime {
        get => Instance._fixedDeltaTime;
        private set => Instance._fixedDeltaTime = value;
    }

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
        Window.Closing += OnClosing;

        Window.Run();
    }

    private void OnLoad () {
        Silk.NET.Input.IInputContext Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Window);
        InputState.Init(Input);

        /*foreach (var keyboard in Input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }*/


        ComponentManager.Init();

        Renderer = new Renderer();
        SceneManager.Init();

        Camera = new CameraEditor();

        engineState = EngineStates.Ready;
    }

    private void OnUpdate (double deltaTime) {
        InputState.Update();
        Inputs.Update();

        if (Inputs.Actions[Inputs.EditorPause].pressedDown) {
            Log.log(engineState);
            if (engineState == EngineStates.Ready) engineState = EngineStates.Paused;
            else if (engineState == EngineStates.Paused) engineState = EngineStates.Ready;
        }
        if (engineState == EngineStates.Ready) {
            _deltaTime = deltaTime;
            _accumulator += deltaTime;

            Update();
            while (fixedDeltaTime <= _accumulator) {
                FixedUpdate();
                _accumulator -= fixedDeltaTime;
            }
        } else {
            ComponentManager.UpdateRender();
        }

        if (Inputs.Actions[Inputs.NavBack].pressed) Window.Close();
    }

    private void FixedUpdate () {
        DataEngine.global_audio_Mult = 1f;

        ComponentManager.FixedUpdate();
        de_FixedUpdate?.Invoke();
    }
    private void Update () {
        ComponentManager.Update();
        de_Update?.Invoke();

        /// Counters
        time += deltaTime;
    }


    public static void SetFixedDeltaTime (double value) {
        if (value <= 0d) {
            Log.log("fixedDeltaTime must be > 0");
            return;
        }
        fixedDeltaTime = value;
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