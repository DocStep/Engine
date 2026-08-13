using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace Engine;


public class CoT {

    public static bool debug = true;

    public string flag = string.Empty;
    public Task? task = null;
    public CancellationTokenSource? cts;
    public bool active = false;
    public bool locked = false;
    public System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    private static readonly object _lock = new object();

    public static ConcurrentDictionary<string, ConcurrentBag<CoT>> Tasks = new();
    public static int Count = 0;

    public static int GetCount() {
        int count = 0;
        foreach (KeyValuePair<string, ConcurrentBag<CoT>> kv in Tasks)
            count += kv.Value.Count;
        return count;
    }

    public async static Task Start(Func<Task> actionFunc, bool locked = false,
        [System.Runtime.CompilerServices.CallerMemberName] string? flag = null) {

        if (actionFunc == null) return;

        flag ??= actionFunc.Method.Name;

        CoT cot = new CoT() {
            flag = flag,
            locked = locked,
            active = true,
            /// cts = new CancellationTokenSource()
        };

        lock (_lock) {
            ConcurrentBag<CoT> bag = Tasks.GetOrAdd(flag, _ => new ConcurrentBag<CoT>());
            bag.Add(cot);
            Count++;
        }

        cot.stopwatch.Start();

        cot.task = Task.Run(async () => {
            try {
                /// await actionFunc(cot.cts.Token);
                await actionFunc();
            }
            catch (Exception e) {
                Log.log($"Task error {flag}: {e}");
            }
            finally {
                cot.active = false;
                cot.stopwatch.Stop();
                Remove(cot);
            }
        });

        await cot.task;
    }

    public static bool isActiveFlag(string flag) {
        return Tasks.ContainsKey(flag);
    }

    public static bool isActiveCoroutine(string flag) {
        if (!Tasks.TryGetValue(flag, out ConcurrentBag<CoT>? bag))
            return false;

        foreach (CoT cot in bag)
            if (cot.task != null)
                return true;

        return false;
    }

    public static CoT? Get(string flag, bool activeCheck = false) {
        if (activeCheck && !isActiveFlag(flag))
            return null;

        if (!Tasks.TryGetValue(flag, out ConcurrentBag<CoT>? bag))
            return null;

        foreach (CoT cot in bag)
            return cot; /// first

        return null;
    }

    private static void Remove(CoT cot) {
        if (!Tasks.TryGetValue(cot.flag, out ConcurrentBag<CoT>? bag))
            return;

        ConcurrentBag<CoT> newBag = new ConcurrentBag<CoT>();
        foreach (CoT c in bag)
            if (c != cot)
                newBag.Add(c);

        lock (_lock) {
            if (newBag.IsEmpty) {
                Tasks.TryRemove(cot.flag, out _);
            } else {
                Tasks[cot.flag] = newBag;
            }

            Count--;
        }
    }

    public static void Stop(string flag, bool forceLocked = false) {
        if (!Tasks.TryGetValue(flag, out ConcurrentBag<CoT>? bag))
            return;

        foreach (CoT cot in bag) {
            if (!Mathf.Implies(cot.locked, forceLocked))
                continue;

            cot.cts?.Cancel(); /// safe
        }
    }

    public static void Stop(CoT cot, bool forceLocked = false) {
        if (!Mathf.Implies(cot.locked, forceLocked))
            return;

        cot.cts?.Cancel();
    }

    public static void StopAll(bool forceLocked = false) {
        foreach (KeyValuePair<string, ConcurrentBag<CoT>> kv in Tasks)
            foreach (CoT cot in kv.Value)
                Stop(cot, forceLocked);
    }

    public static void StopAllWith(string flagPart, bool forceLocked = false) {
        foreach (KeyValuePair<string, ConcurrentBag<CoT>> kv in Tasks) {
            if (!kv.Key.Contains(flagPart))
                continue;

            foreach (CoT cot in kv.Value)
                Stop(cot, forceLocked);
        }
    }

    public static void End_CallBack(CoT cot) {
        cot.stopwatch.Stop();
    }

    public static void WriteAll() {
        string s = string.Empty;

        foreach (KeyValuePair<string, ConcurrentBag<CoT>> kv in Tasks)
            s += kv.Key + ": " + kv.Value.Count + "\n";

        Log.log(s);
    }

}