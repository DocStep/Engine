using Engine.Input;

namespace Engine;


/// Window events
/// FixedUpdate:
///	    Inputs
///     Physics
///     Sync
/// Update
/// Render:



public class Engine : IDisposable {
    public Engine (Type? renderer = null, Type? camera = null, Action? de_Init = null) {
        if (Instance is not null) throw new Exception($"{typeof(Engine)}.{nameof(Instance)} is not null");
        Instance = this;
        Init(renderer, camera, de_Init);
    }

    public static Engine Instance { get; private set; } = null!;

    public Action? de_Update_Engine = null;
    public Action? de_FixedUpdate_Engine = null;
    public static string savesFolder = "Data";

    public Action? de_Update = null;
    public Action? de_FixedUpdate = null;
    public Action? de_Render = null;

    public static Silk.NET.Windowing.IWindow Window = null!;
    public static Silk.NET.Input.IInputContext Input = null!;

    /// Debug
    public EngineStates engineState = EngineStates.Loading;
    public bool debug = false;
    public double sessionTime = 0f;

    //protected EngineStatsUpdate stats = new EngineStatsUpdate();
    public EngineStats Stats = new EngineStats();
    protected System.Diagnostics.Stopwatch sw_LatencyUpdate = new System.Diagnostics.Stopwatch();
    protected System.Diagnostics.Stopwatch sw_LatencyFixedUpdate = new System.Diagnostics.Stopwatch();
    protected System.Diagnostics.Stopwatch sw_LatencySystems = new System.Diagnostics.Stopwatch();


    public void Init (Type? renderer = null, Type? camera = null, Action? de_Init = null) {
        ArgsUtils.Init();

        ThreadUtils.Init();
        Log.CreateSingleton();
        Log.log($"========== Run ==========", LogType.info);
        Log.log($"===== Utils Layer =====", LogType.info);
        Time.Init();
        //CrashHandlers.Init();

        Reflection.CreateSingleton();
        Json.CreateSingleton();

        Log.log($"[{Time.getCurrentTime}]");

        Inputs.OverrideActions(DataEngine.InputsData);

        Inputs.KeysInit();

        ReflectionActionScripts.CreateSingleton();

        Window = Windows.WindowCreate();
        Window.Load += () => OnLoad(renderer ?? typeof(Graphics.Renderer), camera ?? typeof(Graphics.Camera), de_Init);
        Window.Update += OnUpdate;
        Window.Closing += OnClosing;
        de_Render += LogFrameEnd;
    }
    public void Run () => Silk.NET.Windowing.WindowExtensions.Run(Window);

    private void OnLoad (Type rendererType, Type cameraType, Action? de_Init) {
        Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Window);
        InputState.Init(Input);

        Log.log($"===== Systems Layer =====", LogType.info);

        object? renderer = Activator.CreateInstance(rendererType);
        if (renderer as Graphics.Renderer is null) throw new Exception("Renderer is null");

        de_Init?.Invoke();

        Activator.CreateInstance(cameraType);

        PhysicsManager.CreateSingleton();
        ComponentManager.CreateSingleton();
        SceneManager.CreateSingleton();

        //de_Update_Engine?.Invoke();
        engineState = EngineStates.Ready;
        Log.log($"========== Init Finish ==========", LogType.info);

        Engine.SetFPSMax(144);
    }


    private void OnUpdate (double dt) {
        InputState.Update();
        Inputs.Update();

        if (Inputs.Actions[Inputs.EditorPause].pressedDown) {
            if (engineState == EngineStates.Ready) engineState = EngineStates.Paused;
            else if (engineState == EngineStates.Paused) engineState = EngineStates.Ready;
        }
        if (engineState == EngineStates.Ready) {
            Time.deltaTime = dt;
            Time.accumulator += dt;

            Update();
            while (Time.fixedDeltaTime <= Time.accumulator) {
                FixedUpdate();
                Time.accumulator -= Time.fixedDeltaTime;
            }

            f3log();

            de_Render?.Invoke();

            /// Counters
            Time.time += Time.deltaTime;
        } else {
            ComponentManager.Instance.UpdateRender();
        }
    }
    void LogFrameEnd () {
        Stats.LatencyFull = (float)sw_LatencyUpdate.Elapsed.TotalMilliseconds;
        Stats.LatencyRender = Graphics.Renderer.Instance.Stats.Latency;
    }

    private void Update () {
        sw_LatencyUpdate.Restart();

        ComponentManager.Instance.Update();

        de_Update_Engine?.Invoke();
        ReflectionActionScripts.Instance.de_Actions_Update?.Invoke();

        Stats.LatencyUpdate = (float)sw_LatencyUpdate.Elapsed.TotalMilliseconds;
    }
    private void FixedUpdate () {
        sw_LatencyFixedUpdate.Restart();

        DataEngine.global_audio_Mult = 1f;

        sw_LatencySystems.Restart();
        PhysicsManager.Instance.FixedUpdate();
        Stats.LatencyPhysics = (float)sw_LatencySystems.Elapsed.TotalMilliseconds;

        sw_LatencySystems.Restart();
        ComponentManager.Instance.FixedUpdate();
        Stats.LatencyComponents = (float)sw_LatencySystems.Elapsed.TotalMilliseconds;

        de_FixedUpdate_Engine?.Invoke();
        ReflectionActionScripts.Instance.de_Actions_FixedUpdate?.Invoke();

        Stats.LatencyFixedUpdate = (float)sw_LatencyFixedUpdate.Elapsed.TotalMilliseconds;
    }

    public static void SetFixedDeltaTime (double value) {
        if (value <= 0d) {
            Log.log("fixedDeltaTime must be > 0");
            return;
        }

        Time.fixedDeltaTime = value;
    }


    public static void SetFPSMax (double fpsMax) {
        Window.FramesPerSecond = fpsMax;
    }


    void f3log () {
        Graphics.UI.TextRenderer.AddText($"Time: {Time.time:F2}");
        Graphics.UI.TextRenderer.AddText($"FPS: {(int)(1/Time.deltaTime)}");
        Graphics.UI.TextRenderer.AddText($"ms: {Time.deltaTime*1000:F3}");
        Graphics.UI.TextRenderer.AddText($"Pos: {Graphics.Camera.Instance?.cameraPos:F3}");
        Graphics.UI.TextRenderer.AddText($"MousePos: {Graphics.Camera.Instance?.mousePos_Window}");
        Graphics.UI.TextRenderer.AddText($"Components: {ComponentManager.Instance.componentsCount}");
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