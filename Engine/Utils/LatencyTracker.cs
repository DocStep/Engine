using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Measures execution time of void calls, including fire-and-forget
/// async void methods where a normal await-based timer does not work.
/// </summary>
public static class LatencyTracker {
    private static readonly Dictionary<string, Stopwatch> _active = new();

    /// <summary> Starts timing a span identified by id. </summary>
    public static void Start (string id) {
        Stopwatch sw = new();
        sw.Start();
        _active[id] = sw;
    }

    /// <summary> Stops timing id and logs the elapsed time. </summary>
    public static void End (string id, double threshold = 0.01d) {
        if (!_active.TryGetValue(id, out Stopwatch? sw)) {
            Log.log($"[Latency] End called for unknown id: {id}", LogType.warning);
            return;
        }

        sw.Stop();
        _active.Remove(id);

        double ms = sw.Elapsed.TotalMilliseconds;
        if (threshold < ms) Log.log($"[Latency] {id} took {ms:F2} ms");
    }

    public static double Elapsed (string id) {
        if (!_active.TryGetValue(id, out Stopwatch? sw)) {
            Log.log($"[Latency] Elapsed called for unknown id: {id}", LogType.warning);
            return -1;
        }

        return sw.Elapsed.TotalMilliseconds;
    }

    public static void Write (string id, double ms) {
        Log.log($"[Latency] {id} took {ms:F2} ms");
    }


    /// <summary> Times a synchronous void action in one call. </summary>
    public static void Measure (string id, Action action) {
        Start(id);
        action();
        End(id);
    }

}