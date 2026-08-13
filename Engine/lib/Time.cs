using System.Diagnostics;

namespace Engine;


public static class Time {

    public static void Init () {
        startTick = Stopwatch.GetTimestamp();
        startTime = DateTime.UtcNow;
        //Console.WriteLine($"Inited: {nameof(TimeUtils)}");
    }


    static long startTick;
    static DateTime startTime;

    public static string getCurrentTime {
        get {
            //long t = Stopwatch.GetTimestamp();
            //long dt = t - startTick;
            //long ticks = dt * TimeSpan.TicksPerSecond / Stopwatch.Frequency;
            //DateTime dateTime = startTime + new TimeSpan(ticks);

            DateTime now = DateTime.Now;

            //string timeLog = $" (t: {dateTime:HH:mm:ss.fffffff} | {now:HH:mm:ss.fffffff})";
            string timeLog = $"{now:HH:mm:ss.fffffff}";
            return timeLog;
        }
    }
    public static string getCurrentTimeLog {
        get {
            //long t = Stopwatch.GetTimestamp();
            //long dt = t - startTick;
            //long ticks = dt * TimeSpan.TicksPerSecond / Stopwatch.Frequency;
            //DateTime dateTime = startTime + new TimeSpan(ticks);

            DateTime now = DateTime.UtcNow;

            //string timeLog = $" (t: {dateTime:HH:mm:ss.fffffff} | {now:HH:mm:ss.fffffff})";
            string timeLog = $"{now:HH:mm:ss.fffffff}";
            return timeLog;
        }
    }


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

    public static int FPS => (int)(0 < Windows.Window.FramesPerSecond ? Windows.Window.FramesPerSecond : (int)(1/Time.deltaTime));

}
