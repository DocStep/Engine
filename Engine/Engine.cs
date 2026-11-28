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
    public Engine (EngineEntryPoint? entryPoint = null) {
        if (Instance is not null) throw new Exception($"{typeof(Engine)}.{nameof(Instance)} is not null");
        Instance = this;
        Init(entryPoint);
    }

    public static Engine Instance { get; private set; } = null!;

    //public Action? de_Update_Engine = null;
    //public Action? de_FixedUpdate_Engine = null;
    public static string savesFolder = "Data";

    public Action? de_StateReset = null;
    public Action? de_Update = null;
    public Action? de_UpdateAlways = null;
    public Action? de_AfterUpdate = null;
    public Action? de_FixedUpdate = null;
    public Action? de_Render = null;
    public Action? de_LateUpdate = null;

    public Action? de_Focus = null;
    public Action? de_Unfocus = null;

    public Action? de_Closing = null;
    public Func<bool>? de_ClosingRequest = null;

    /// Debug
    public EngineStates EngineState = EngineStates.Loading;
    public bool debug = false;
    public double sessionTime = 0f;

    //protected EngineStatsUpdate stats = new EngineStatsUpdate();
    public EngineStats Stats = new();
    protected System.Diagnostics.Stopwatch sw_LatencyUpdate = new();
    protected System.Diagnostics.Stopwatch sw_LatencyFixedUpdate = new();
    protected System.Diagnostics.Stopwatch sw_LatencySystems = new();


    private EngineLaunchOptions launchOptions = null!;
    private bool isEditorLaunch;

    private void Init (EngineEntryPoint? entryPoint) {
        string[] commandLineArguments = Environment.GetCommandLineArgs();
        string[] arguments = entryPoint?.Arguments ?? (commandLineArguments.Length > 1 ? commandLineArguments[1..] : Array.Empty<string>());
        launchOptions = EngineLaunchOptions.Parse(arguments);
        isEditorLaunch = launchOptions.IsEditor || entryPoint?.Editor == true;
        ArgsUtils.Init(launchOptions.Arguments);
        if (isEditorLaunch) ArgsUtils.isEditor = true;

        ThreadUtils.Init();
        Log.Init();
        Log.log($"========== Run ==========", LogType.system);
        Log.log($"===== Utils Layer =====", LogType.system);
        Time.Init();
        //CrashHandlers.Init();

        //Reflection.CreateSingleton();
        Json.Init();

        Log.log($"[{Time.CurrentTime}]");

        Inputs.OverrideActions(DataEngine.InputsData);

        Inputs.KeysInit();

        //ReflectionActionScripts.CreateSingleton();

        Windows.Window = Windows.WindowCreate();
        Windows.Window.Load += () => OnLoad(entryPoint);
        Windows.Window.Update += OnUpdate;
        Windows.Window.Closing += OnClosing;
    }
    public void Run () {
        Silk.NET.Windowing.WindowExtensions.Run(Windows.Window);
    }

    private void OnLoad (EngineEntryPoint? entryPoint) {
        Windows.Input = Silk.NET.Input.InputWindowExtensions.CreateInput(Windows.Window);
        WindowInput.Init(Windows.Input);

        Log.log($"===== Systems Layer =====", LogType.system);

        Type? rendererType = isEditorLaunch
            ? entryPoint?.EditorRenderer ?? entryPoint?.Renderer
            : entryPoint?.Renderer;
        rendererType ??= typeof(Graphics.Renderer);
        if (Activator.CreateInstance(rendererType) is not Graphics.Renderer)
            throw new Exception("Renderer is null");

        Physics.CreateSingleton();
        ComponentsManager.CreateSingleton();
        SceneManager.CreateSingleton();

        Log.log($"===== Hook Layer =====", LogType.system);

        if (isEditorLaunch) entryPoint?.OnEditorLoad?.Invoke();
        entryPoint?.OnLoad?.Invoke();
        if (SceneManager.Instance.Scenes.Count == 0) LoadStartScene();
        ReflectionActionScripts.CreateSingleton();

        EngineState = isEditorLaunch ? EngineStates.Paused : EngineStates.Ready;
        Log.log($"========== Init Finish ==========", LogType.system);

        //Engine.SetFPSMax(144);
    }

    private void LoadStartScene () {
        if (string.IsNullOrWhiteSpace(launchOptions.ScenePath)) {
            new Scene() { Name = "Empty Scene" };
            return;
        }

        string scenePath;
        if (Path.IsPathRooted(launchOptions.ScenePath)) {
            scenePath = Path.HasExtension(launchOptions.ScenePath)
                ? launchOptions.ScenePath : launchOptions.ScenePath + ".json";
        } else if (!launchOptions.ScenePath.Contains(Path.DirectorySeparatorChar)
                && !launchOptions.ScenePath.Contains(Path.AltDirectorySeparatorChar)) {
            scenePath = Path.Combine(Dirs.Scenes, Path.GetFileNameWithoutExtension(launchOptions.ScenePath) + ".json");
        } else {
            scenePath = Path.Combine(Dirs.Root, Path.HasExtension(launchOptions.ScenePath)
                ? launchOptions.ScenePath
                : launchOptions.ScenePath + ".json");
        }

        if (Scene.Load(scenePath) is null) {
            Log.log($"Start scene not found: {scenePath}. Creating an empty scene.", LogType.warning);
            new Scene() { Name = "Empty Scene" };
        }
    }

    //Audio.AudioManager audio;
    //Audio.AudioClip step;

    private void OnUpdate (double dt) {
        de_StateReset?.Invoke();
        sw_LatencyUpdate.Restart();

        WindowInput.Update();
        Inputs.Update();

        if (Inputs.Actions[Inputs.EditorPause].pressedDown) {
            if (EngineState != EngineStates.Paused) EngineState = EngineStates.Paused;
            else if (EngineState == EngineStates.Paused) EngineState = EngineStates.Ready;
        }

        Time.isPaused = EngineState == EngineStates.Paused;
        Time.Update(dt);

        if (EngineState == EngineStates.Ready) {
            Time.accumulator += Time.deltaTime;

            Update();
            while (Time.fixedDeltaTime <= Time.accumulator) {
                Time.FixedUpdateStep();
                FixedUpdate();
                Time.accumulator -= Time.fixedDeltaTime;
            }
        } else {
            de_UpdateAlways?.Invoke();
        }

        F3_Log();

        de_Render?.Invoke();
        LogFrameEnd();

        de_LateUpdate?.Invoke();
    }
    void LogFrameEnd () {
        Stats.LatencyFull = (float)sw_LatencyUpdate.Elapsed.TotalMilliseconds;
        Stats.LatencyRender = Graphics.Renderer.Instance.Stats.Latency;
    }

    private void Update () {
        ComponentsManager.Instance.Update();

        de_Update?.Invoke();
        ReflectionActionScripts.Instance?.de_Actions_Update?.Invoke();
        de_UpdateAlways?.Invoke();

        //if (Renderer.Instance.Camera is not null) {
        //    Transform tr = Renderer.Instance.Camera.gameObject.Transform;
        //    audio.UpdateListener(Renderer.Instance.Camera.CameraPos, Vector3.UnitZ, Vector3.UnitY);
        //}

        de_AfterUpdate?.Invoke();

        Stats.LatencyUpdate = (float)sw_LatencyUpdate.Elapsed.TotalMilliseconds;
    }
    private void FixedUpdate () {
        sw_LatencyFixedUpdate.Restart();

        DataEngine.global_audio_Mult = 1f;

        sw_LatencySystems.Restart();
        Physics.Instance.FixedUpdate();
        Stats.LatencyPhysics = (float)sw_LatencySystems.Elapsed.TotalMilliseconds;

        sw_LatencySystems.Restart();
        ComponentsManager.Instance.FixedUpdate();
        Stats.LatencyComponents = (float)sw_LatencySystems.Elapsed.TotalMilliseconds;

        de_FixedUpdate?.Invoke();
        ReflectionActionScripts.Instance?.de_Actions_FixedUpdate?.Invoke();

        Stats.LatencyFixedUpdate = (float)sw_LatencyFixedUpdate.Elapsed.TotalMilliseconds;
    }


    public static void SetFPSMax (double fpsMax) {
        Windows.Window.FramesPerSecond = fpsMax;
    }


    void F3_Log () {
        Graphics.UI.TextRenderer.AddText($"Time: {Time.time:F2}");
        Graphics.UI.TextRenderer.AddText($"FPS: {Time.FPS}");
        Graphics.UI.TextRenderer.AddText($"ms: {Time.deltaTime*1000:F3}");
        Graphics.UI.TextRenderer.AddText($"Components: {ComponentsManager.Instance.componentsCount}");
        Graphics.UI.TextRenderer.AddText($"SceneSize: {Graphics.Renderer.Instance.Stats.SceneSize}");
        Graphics.UI.TextRenderer.AddText($"MousePos: {Inputs.MousePos}");
        Graphics.UI.TextRenderer.AddText($"MousePos_Window: {Inputs.MousePos_Window}");
        Graphics.UI.TextRenderer.AddText($"isMouseOver: {Inputs.isMouseOver}");
        Graphics.UI.TextRenderer.AddText($"isMouseOver_Window: {Inputs.isMouseOver_Window}");
    }


    /*private void OnKeyDown (IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Window.Close();
        }
    }*/

    private void OnClosing () {
        if (de_ClosingRequest?.Invoke() == false) {
            Windows.Window.IsClosing = false;
            return;
        }

        de_Closing?.Invoke();
        Console.ResetColor();
    }

    public void Dispose () {
        Windows.Window?.Dispose();
    }

}