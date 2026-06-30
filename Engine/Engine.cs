using Silk.NET.Windowing;
using Engine.Graphics;
using Engine.Input;

namespace Engine;


public class Engine : Singleton<Engine>, IDisposable {

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
        get {
            Log.log($"Instance id: {Instance.GetHashCode()}");
            return Instance._deltaTime;
        }
        private set => Instance._deltaTime = value;
    }

    private double _fixedDeltaTime = 1d/50d;
    public static double fixedDeltaTime {
        get => Instance._fixedDeltaTime;
        private set => Instance._fixedDeltaTime = value;
    }

    public void Run () {
        ArgsUtils.Args();

        Console.WriteLine($"========== Init ==========");
        Console.WriteLine($"===== Utils Layer =====");

        ThreadUtils.Init();
        Log.InstanceNew();
        TimeUtils.Init();
        Log.log($"Time: {TimeUtils.getCurrentTime}");
        Reflection.InstanceNew();
        Json.InstanceNew();

        Inputs.OverrideActions(DataEngine.InputsData);

        Inputs.KeysInit();

        //Console.WriteLine($"========== Init Finish ==========");

        var options = WindowOptions.Default with {
            Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
            Title = "Engine",
            VSync = false,
            WindowBorder = WindowBorder.Resizable,
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


        Console.WriteLine($"===== Systems Layer =====");

        PhysicsManager.InstanceNew();
        ComponentManager.InstanceNew();

        Renderer = new Renderer();
        SceneManager.InstanceNew();

        Camera = new CameraEditor();

        Console.WriteLine($"========== Init Finish ==========");

        engineState = EngineStates.Ready;
    }

    private void OnUpdate (double dt) {
        InputState.Update();
        Inputs.Update();

        if (Inputs.Actions[Inputs.EditorPause].pressedDown) {
            if (engineState == EngineStates.Ready) engineState = EngineStates.Paused;
            else if (engineState == EngineStates.Paused) engineState = EngineStates.Ready;
        }
        if (engineState == EngineStates.Ready) {
            _deltaTime = dt;
            _accumulator += dt;

            Update();
            while (fixedDeltaTime <= _accumulator) {
                FixedUpdate();
                _accumulator -= fixedDeltaTime;
            }
        } else {
            ComponentManager.Instance.UpdateRender();
        }

        if (Inputs.Actions[Inputs.NavBack].pressed) Window.Close();
    }

    private void FixedUpdate () {
        DataEngine.global_audio_Mult = 1f;

        ComponentManager.Instance.FixedUpdate();
        PhysicsManager.Instance.FixedUpdate();
        de_FixedUpdate?.Invoke();
    }
    private void Update () {
        ComponentManager.Instance.Update();
        //de_Update?.Invoke();

        Graphics.UI.TextRenderer.AddText($"Time: {time:F2}");
        Graphics.UI.TextRenderer.AddText($"FPS: {(int)(1/Engine.Instance._deltaTime)}");
        Graphics.UI.TextRenderer.AddText($"ms: {_deltaTime*1000:F3}");
        Graphics.UI.TextRenderer.AddText($"Pos: {Camera.Instance?.cameraPos:F3}");
        Graphics.UI.TextRenderer.AddText($"MousePos: {Camera.Instance?.mousePos}");
        Graphics.UI.TextRenderer.AddText($"Components: {ComponentManager.Instance.componentsCount}");

        /// Counters
        time += _deltaTime;
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