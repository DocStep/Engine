using System.Linq;
using Engine.Input;

namespace Engine;


public class Engine : Singleton<Engine>, IDisposable {

    public Action? de_Update = null;
    public Action? de_FixedUpdate = null;
    public static string savesFolder = "Data";

    internal static Silk.NET.Windowing.IWindow Window = null!;

    private Graphics.Renderer Renderer = null!;
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
        Log.InstanceCheck();
        TimeUtils.Init();
        Log.log($"Time: {TimeUtils.getCurrentTime}");
        Reflection.InstanceCheck();
        Json.InstanceCheck();
        AssetsManager.Init();

        Inputs.OverrideActions(DataEngine.InputsData);

        Inputs.KeysInit();

        //Console.WriteLine($"========== Init Finish ==========");

        var options = Silk.NET.Windowing.WindowOptions.Default with {
            Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
            Title = "Engine",
            VSync = false,
            WindowBorder = Silk.NET.Windowing.WindowBorder.Resizable,
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

        Silk.NET.Windowing.WindowExtensions.Run(Window);
    }

    private void OnLoad () {
        Silk.NET.Input.IInputContext Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Window);
        InputState.Init(Input);

        /*foreach (var keyboard in Input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }*/

        Console.WriteLine($"===== Systems Layer =====");

        Renderer = new Graphics.Renderer();

        PhysicsManager.InstanceCheck();
        ComponentManager.InstanceCheck();
        SceneManager.InstanceCheck();

        Camera = new CameraEditor();

        de_Update?.Invoke();

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
        de_Update?.Invoke();

        /// Counters
        time += _deltaTime;

        f3log();
    }

    void f3log () {
        Graphics.UI.TextRenderer.AddText($"Time: {time:F2}");
        Graphics.UI.TextRenderer.AddText($"FPS: {(int)(1/Engine.Instance._deltaTime)}");
        Graphics.UI.TextRenderer.AddText($"ms: {_deltaTime*1000:F3}");
        Graphics.UI.TextRenderer.AddText($"Pos: {Camera.Instance?.cameraPos:F3}");
        Graphics.UI.TextRenderer.AddText($"MousePos: {Camera.Instance?.mousePos}");
        Graphics.UI.TextRenderer.AddText($"Components: {ComponentManager.Instance.componentsCount}");
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