using System.Diagnostics;

namespace Engine;

public static class Time {

    public static void Init () {
        startTick = Stopwatch.GetTimestamp();
        startTime = DateTime.UtcNow;
    }

    [Hide] static long startTick;
    [Hide] static DateTime startTime;
    [Hide] public static double accumulator = 0d;

    [Hide] public const string TimeFormat = "HH:mm:ss.fffffff";


    /// --- Pause / scale control ---
    public static bool isPaused = false;
    public static double timeScale = 1d;

    [Readonly] public static int FPS => 
        (int)(0 < Windows.Window.FramesPerSecond ? Windows.Window.FramesPerSecond : (int)(1/Time.unscaledDeltaTime));


    /// --- Game time: stops/scales with isPaused / timeScale ---
    [InspectorName("Time")] private static double _time = 0d;
    [Hide] public static double time {
        get => _time;
        internal set => _time = value;
    }

    [InspectorName("DeltaTime")] private static double _deltaTime;
    [Hide] public static double deltaTime {
        get => _deltaTime;
        internal set => _deltaTime = value;
    }

    [InspectorName("FixedDeltaTime")] private static double _fixedDeltaTime = 1d/50d;
    [Hide] public static double fixedDeltaTime {
        get => _fixedDeltaTime;
        internal set => _fixedDeltaTime = value;
    }

    /// --- Fixed-step time: advances once per FixedUpdate call ---
    [InspectorName("FixedTime")] private static double _fixedTime = 0d;
    [Hide] public static double fixedTime {
        get => _fixedTime;
        internal set => _fixedTime = value;
    }
    [Hide] private static long _fixedFrameCount = 0;
    [Hide] public static long fixedFrameCount {
        get => _fixedFrameCount;
        internal set => _fixedFrameCount = value;
    }

    /// --- Real time: never stops, drives the engine loop, editor UI, etc. ---
    [InspectorName("UnscaledTime")] private static double _unscaledTime = 0d;
    [Hide] public static double unscaledTime {
        get => _unscaledTime;
        internal set => _unscaledTime = value;
    }
    [InspectorName("UnscaledDeltaTime")] private static double _unscaledDeltaTime;
    [Hide] public static double unscaledDeltaTime {
        get => _unscaledDeltaTime;
        internal set => _unscaledDeltaTime = value;
    }

    
    [Readonly] public static string getCurrentTime {
        get {
            DateTime now = DateTime.Now;
            return now.ToString(TimeFormat);
        }
    }
    [Readonly] public static string getCurrentTimeLog {
        get {
            DateTime now = DateTime.UtcNow;
            return now.ToString(TimeFormat);
        }
    }


    /// --- Called once per frame by the engine loop with the raw, unscaled delta ---
    internal static void Update (double rawDeltaTime) {
        unscaledDeltaTime = rawDeltaTime;
        unscaledTime += rawDeltaTime;

        double scale = isPaused ? 0d : timeScale;
        deltaTime = rawDeltaTime * scale;
        time += deltaTime;
    }

    /// --- Called once per fixed step by the engine loop ---
    internal static void FixedUpdateStep () {
        fixedTime += fixedDeltaTime;
        fixedFrameCount++;
    }


}