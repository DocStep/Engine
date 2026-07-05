using System.Diagnostics;

namespace Engine;


public static class TimeUtils {

    public static void Init () {
        startTick = Stopwatch.GetTimestamp();
        startTime = DateTime.UtcNow;
        Log.log($"Inited: {nameof(TimeUtils)}");
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


}
