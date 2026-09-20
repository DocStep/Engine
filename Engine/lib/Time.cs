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

    [Hide] public const string TimeFormat = "HH:mm:ss.ff";


    /// --- Pause / scale control ---
    public static bool isPaused = false;
    public static double timeScale = 1d;

    [Readonly] public static int FPS => 
        (int)(0 < Windows.Window.FramesPerSecond ? Windows.Window.FramesPerSecond : (int)(1/unscaledDeltaTime));


    /// --- Game time: stops/scales with isPaused / timeScale ---
    [Hide] private static double _time = 0d;
    [Separator][InspectorName("Time")]
    public static double time {
        get => _time;
        internal set => _time = value;
    }

    [Hide] private static double _deltaTime;
    [InspectorName("DeltaTime")] 
    public static double deltaTime {
        get => _deltaTime;
        internal set => _deltaTime = value;
    }

    [Hide] private static double _fixedDeltaTime = 1d/50d;
    [InspectorName("FixedDeltaTime")]
    public static double fixedDeltaTime {
        get => _fixedDeltaTime;
        internal set => SetFixedDeltaTime(value);
    }

    /// --- Fixed-step time: advances once per FixedUpdate call ---
    [Hide] private static double _fixedTime = 0d;
    [Separator, InspectorName("FixedTime")]
    public static double fixedTime {
        get => _fixedTime;
        internal set => _fixedTime = value;
    }
    [Hide] private static long _fixedFrameCount = 0;
    [Hide] public static long fixedFrameCount {
        get => _fixedFrameCount;
        internal set => _fixedFrameCount = value;
    }

    /// --- Real time: never stops, drives the engine loop, editor UI, etc. ---
    [Hide] private static double _unscaledTime = 0d;
    [Separator, InspectorName("UnscaledTime")]
    public static double unscaledTime {
        get => _unscaledTime;
        internal set => _unscaledTime = value;
    }
    [Hide] private static double _unscaledDeltaTime;
    [InspectorName("UnscaledDeltaTime")]
    public static double unscaledDeltaTime {
        get => _unscaledDeltaTime;
        internal set => _unscaledDeltaTime = value;
    }

    
    [Separator, Readonly]
    public static string CurrentTime {
        get {
            DateTime now = DateTime.Now;
            return now.ToString(TimeFormat);
        }
    }
    [Readonly]
    public static string CurrentTimeLog {
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

    public static void SetFixedDeltaTime (double value) {
        const double minValue = 0.01d;
        if (value < minValue) value = minValue;
        _fixedDeltaTime = value;
        Log.log(_fixedDeltaTime);
    }

}