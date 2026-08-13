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
    public Engine (Type? renderer = null, Action? de_Init = null) {
        if (Instance is not null) throw new Exception($"{typeof(Engine)}.{nameof(Instance)} is not null");
        Instance = this;
        Init(renderer, de_Init);
    }

    public static Engine Instance { get; private set; } = null!;

    //public Action? de_Update_Engine = null;
    //public Action? de_FixedUpdate_Engine = null;
    public static string savesFolder = "Data";

    public Action? de_Update = null;
    public Action? de_FixedUpdate = null;
    public Action? de_StateReset = null;
    public Action? de_Render = null;

    /// Debug
    public EngineStates engineState = EngineStates.Loading;
    public bool debug = false;
    public double sessionTime = 0f;

    //protected EngineStatsUpdate stats = new EngineStatsUpdate();
    public EngineStats Stats = new EngineStats();
    protected System.Diagnostics.Stopwatch sw_LatencyUpdate = new System.Diagnostics.Stopwatch();
    protected System.Diagnostics.Stopwatch sw_LatencyFixedUpdate = new System.Diagnostics.Stopwatch();
    protected System.Diagnostics.Stopwatch sw_LatencySystems = new System.Diagnostics.Stopwatch();


    public void Init (Type? renderer = null, Action? de_Init = null) {
        ArgsUtils.Init();

        ThreadUtils.Init();
        Log.CreateSingleton();
        Log.log($"========== Run ==========", LogType.warning);
        Log.log($"===== Utils Layer =====", LogType.warning);
        Time.Init();
        //CrashHandlers.Init();

        //Reflection.CreateSingleton();
        Json.CreateSingleton();

        Log.log($"[{Time.getCurrentTime}]");

        Inputs.OverrideActions(DataEngine.InputsData);

        Inputs.KeysInit();

        ReflectionActionScripts.CreateSingleton();

        Windows.Window = Windows.WindowCreate();
        Windows.Window.Load += () => OnLoad(renderer ?? typeof(Graphics.Renderer), de_Init);
        Windows.Window.Update += OnUpdate;
        Windows.Window.Closing += OnClosing;
    }
    public void Run () => Silk.NET.Windowing.WindowExtensions.Run(Windows.Window);

    private void OnLoad (Type rendererType, Action? de_Init) {
        Windows.Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Windows.Window);
        WindowInput.Init(Windows.Input);

        Log.log($"===== Systems Layer =====", LogType.warning);

        object? renderer = Activator.CreateInstance(rendererType);
        if (renderer as Graphics.Renderer is null) throw new Exception("Renderer is null");

        PhysicsManager.CreateSingleton();
        ComponentManager.CreateSingleton();
        SceneManager.CreateSingleton();

        Log.log($"===== Hook Layer =====", LogType.warning);

        de_Init?.Invoke();
        //ReflectionActionScripts.CreateSingleton();

        engineState = EngineStates.Ready;
        Log.log($"========== Init Finish ==========", LogType.warning);

        Engine.SetFPSMax(144);
    }


    private void OnUpdate (double dt) {
        de_StateReset?.Invoke();

        WindowInput.Update();
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

            LogFrameEnd();

            /// Counters
            Time.time += Time.deltaTime;
        } else {
            //ComponentManager.Instance.UpdateAtFreeze();
        }
    }
    void LogFrameEnd () {
        Stats.LatencyFull = (float)sw_LatencyUpdate.Elapsed.TotalMilliseconds;
        Stats.LatencyRender = Graphics.Renderer.Instance.Stats.Latency;
    }

    private void Update () {
        sw_LatencyUpdate.Restart();

        ComponentManager.Instance.Update();

        de_Update?.Invoke();
        ReflectionActionScripts.Instance?.de_Actions_Update?.Invoke();

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

        de_FixedUpdate?.Invoke();
        ReflectionActionScripts.Instance?.de_Actions_FixedUpdate?.Invoke();

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
        Windows.Window.FramesPerSecond = fpsMax;
    }


    void f3log () {
        Graphics.UI.TextRenderer.AddText($"Time: {Time.time:F2}");
        //Graphics.UI.TextRenderer.AddText($"FPS: {(int)(1/Time.deltaTime)}");
        Graphics.UI.TextRenderer.AddText($"FPS: {Time.FPS}");
        Graphics.UI.TextRenderer.AddText($"ms: {Time.deltaTime*1000:F3}");
        Graphics.UI.TextRenderer.AddText($"Components: {ComponentManager.Instance.componentsCount}");
        Graphics.UI.TextRenderer.AddText($"SceneSize: {Graphics.Renderer.Instance.Stats.SceneSize}");
        Graphics.UI.TextRenderer.AddText($"MousePos_Window: {Inputs.MousePos}");
        Graphics.UI.TextRenderer.AddText($"MousePos_Scene: {Inputs.MousePos_Scene}");
        Graphics.UI.TextRenderer.AddText($"isMouseOver: {Inputs.isMouseOver}");
        Graphics.UI.TextRenderer.AddText($"isMouseOverScene: {Inputs.isMouseOverScene}");
    }


    /*private void OnKeyDown (IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Window.Close();
        }
    }*/

    private void OnClosing () {
        Console.ResetColor();
    }

    public void Dispose () {
        Windows.Window?.Dispose();
    }

}