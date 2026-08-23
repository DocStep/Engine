using System.Diagnostics;

namespace Engine;

public static class Time {

    public static void Init () {
        startTick = Stopwatch.GetTimestamp();
        startTime = DateTime.UtcNow;


    }

    static long startTick;
    static DateTime startTime;

    public const string TimeFormat = "HH:mm:ss.fffffff";

    // --- pause / scale control ---
    public static bool isPaused = false;
    public static double timeScale = 1d;

    // --- real time: never stops, drives the engine loop, editor UI, etc. ---
    private static double _unscaledDeltaTime;
    public static double unscaledDeltaTime {
        get => _unscaledDeltaTime;
        internal set => _unscaledDeltaTime = value;
    }

    private static double _unscaledTime = 0d;
    public static double unscaledTime {
        get => _unscaledTime;
        internal set => _unscaledTime = value;
    }

    // --- game time: stops/scales with isPaused / timeScale ---
    private static double _time = 0d;
    public static double time {
        get => _time;
        internal set => _time = value;
    }

    private static double _deltaTime;
    public static double deltaTime {
        get => _deltaTime;
        internal set => _deltaTime = value;
    }

    internal static double accumulator = 0d;

    private static double _fixedDeltaTime = 1d/50d;
    public static double fixedDeltaTime {
        get => _fixedDeltaTime;
        internal set => _fixedDeltaTime = value;
    }

    // --- called once per frame by the engine loop with the raw, unscaled delta ---
    internal static void Update (double rawDeltaTime) {
        unscaledDeltaTime = rawDeltaTime;
        unscaledTime += rawDeltaTime;

        double scale = isPaused ? 0d : timeScale;
        deltaTime = rawDeltaTime * scale;
        time += deltaTime;
    }

    public static string getCurrentTime {
        get {
            DateTime now = DateTime.Now;
            return now.ToString(TimeFormat);
        }
    }
    public static string getCurrentTimeLog {
        get {
            DateTime now = DateTime.UtcNow;
            return now.ToString(TimeFormat);
        }
    }

    public static int FPS => (int)(0 < Windows.Window.FramesPerSecond ? Windows.Window.FramesPerSecond : (int)(1/Time.unscaledDeltaTime));
}