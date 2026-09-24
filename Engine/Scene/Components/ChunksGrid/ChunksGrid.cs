/// 2D chunk grid (Minecraft-style column loading). Each layer streams on its own radius
/// against a shared shape (IsCircle); load/save/unload run as cancelable, budgeted, priority-
/// ordered tasks so heavy work (gen, IO) never has to block a frame - see ProcessTasks().
/// 3D later = swap Vector2Int for a 3-axis coord in OffsetFromCenter/WorldToChunk/SpiralOffsets,
/// the task/dependency/priority machinery doesn't change.

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Engine;

public enum ChunkState { None, Loading, Ready, Saving, Unloading }
public enum ChunkTaskType { Load, Save, Unload }


public class ChunksGrid : Component, IUpdate {

    public override string Name => nameof(ChunksGrid);

    /// Events
    public event Action<ChunkLayer, Vector2Int>? de_ChunkLoaded;
    public event Action<ChunkLayer, Vector2Int>? de_ChunkUnloaded;
    public event Action? de_ChunksLoaded;   /// permanent mode only - fires once every layer's initial load finishes
    public event Action? de_ChunksUnloaded; /// UnloadAll

    [JsonIgnore] public static Transform Transform { get; private set; } = null!;
    [JsonIgnore] public static Transform TransformTarget { get; private set; } = null!;

    /// Config
    public bool IsPermanentChunks = false; /// true = load full extent once per layer, never streams/unloads on move
    public bool IsCircle = true;          /// false = quad
    public Vector3 Center = Vector3.Zero;
    public static int ChunkSize = 10;
    public int MaxTasksStartedPerTick = 32; /// budget - call ProcessTasks() once per frame
    public int MaxUnloadTasksStartedPerTick = 16; /// reserved floor for unloads specifically - see ProcessTasks()

    public readonly List<ChunkLayer> Layers = new List<ChunkLayer>();

    [Hide] private ChunkLayer[] _loadOrder = Array.Empty<ChunkLayer>();
    [Hide] private ChunkLayer[] _unloadOrder = Array.Empty<ChunkLayer>();
    [Hide] private readonly Dictionary<ChunkLayer, int> _loadRank = new Dictionary<ChunkLayer, int>();
    [Hide] private readonly Dictionary<ChunkLayer, int> _unloadRank = new Dictionary<ChunkLayer, int>();
    [Hide] private readonly Dictionary<ChunkLayer, List<ChunkLayer>> _dependents = new();
    [Hide] private readonly HashSet<Vector2Int> _requiredScratch = new HashSet<Vector2Int>();
    [Hide] private readonly HashSet<Vector2Int> _existingScratch = new HashSet<Vector2Int>();

    [Hide] private readonly List<ChunkTask> _pending = new List<ChunkTask>();
    [Hide] private Vector2Int _lastCenterChunk;
    [Hide] private bool _initialized;
    [Hide] private int _initialLoadRemaining;

    [Hide] private readonly Dictionary<ChunkLayer, List<ChunkLayer>> _dependencyLayers = new();

    
    /// Register layers before the first UpdateCenter call.
    public void AddLayer (ChunkLayer layer) {
        Layers.Add(layer);
        RebuildLayerOrder();
    }


    public override void OnAdd () {
        Transform = gameObject.Transform;
        TransformTarget = new GameObject() { Name = "ChunksGridTarget", }.Transform;
        RebuildLayerOrder();

        de_ChunksLoaded += save;
    }
    public override void OnRemove () {
        de_ChunksLoaded -= save;
    }
    void save () {
        //System.Threading.Thread.Sleep(1000);
        //Log.log("de_ChunksLoaded", Transform.Children.Count);
        gameObject.Save("src/Prefabs/ChunksGrid.json");
    }

    public void Update () {
        Vector3 targetPos = TransformTarget.Position;
        UpdateCenter(targetPos);
        //ForceResync();
        ProcessTasks();
    }
    /// Call on spawn and whenever the streaming center moves (player position, etc).
    /// Only enqueues work - pair with ProcessTasks() every frame or nothing actually runs.
    public void UpdateCenter (Vector3 newCenter) {
        Center = newCenter;

        if (IsPermanentChunks && _initialized) return;

        //Log.log("1");
        Vector2Int centerChunk = WorldToChunk(newCenter);
        if (_initialized && centerChunk.Equals(_lastCenterChunk)) return;

        //Log.log("2");
        _lastCenterChunk = centerChunk;
        bool firstSync = !_initialized;
        _initialized = true;
        Resync(firstSync);
    }

    /// Call after changing a layer's Radius (or similar config) at runtime to re-evaluate immediately.
    public void ForceResync () => Resync(false);

    /// Steps the task queue - starts up to MaxTasksStartedPerTick new tasks (best priority first),
    /// then finalizes any that completed since last call. Call once per frame.
    public void ProcessTasks () {
        int unloadBudget = MaxUnloadTasksStartedPerTick;
        int loadBudget = MaxTasksStartedPerTick - MaxUnloadTasksStartedPerTick;
        int started = 0;

        // Pass 1: fill each side's reserved floor first, so neither can be starved by the other.
        while (unloadBudget > 0 && started < MaxTasksStartedPerTick) {
            ChunkTask? task = PickNextTask(ChunkTaskType.Unload);
            if (task is null) break;
            TaskStart(task);
            unloadBudget--;
            started++;
        }
        while (loadBudget > 0 && started < MaxTasksStartedPerTick) {
            ChunkTask? task = PickNextTask(null); // Load or Save
            if (task is null) break;
            TaskStart(task);
            loadBudget--;
            started++;
        }

        // Pass 2: anything still unspent this tick goes to whichever side has waiting work,
        // highest overall priority first - no wasted slots if one side ran dry early.
        while (started < MaxTasksStartedPerTick) {
            ChunkTask? task = PickNextTask(null);
            if (task is null) break;
            TaskStart(task);
            started++;
        }

        for (int i = _pending.Count - 1; i >= 0; i--) {
            ChunkTask t = _pending[i];
            if (t.Started && t.Running!.IsCompleted) {
                Finish(t);
                _pending.RemoveAt(i);
            }
        }

        //Log.log("_pending", _pending.Count);
    }


    /// Queues a save for a currently-loaded chunk. No-op if a load/unload is already pending.
    public void RequestSave (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.ContainsKey(coord)) return;
        if (layer.GetState(coord) != ChunkState.Ready) return;
        Enqueue(layer, coord, ChunkTaskType.Save);
    }

    /// Full teardown (level unload, grid reset...). Blocking - for explicit teardown, not the streaming path.
    public void UnloadAll () {
        _pending.Clear();

        foreach (ChunkLayer layer in _unloadOrder) {
            foreach (Vector2Int coord in new List<Vector2Int>(layer.States.Keys)) {
                layer.RunUnload(coord).GetAwaiter().GetResult();
                layer.States.Remove(coord);
                de_ChunkUnloaded?.Invoke(layer, coord);
            }
            layer.Pending.Clear();
        }

        _initialized = false;
        de_ChunksUnloaded?.Invoke();
    }

    private void Resync (bool firstSync) {
        //VoidLatencyChecker.Start("ChunksGrid.Resync");
        int enqueuedLoads = 0;

        foreach (ChunkLayer layer in _loadOrder) {
            _requiredScratch.Clear();
            foreach (Vector2Int offset in SpiralOffsets(RingCount(layer.Radius))) {
                Vector2Int coord = _lastCenterChunk + offset;
                if (IsInRange(coord, layer.Radius)) {
                    _requiredScratch.Add(coord);
                    if (RequestLoad(layer, coord)) enqueuedLoads++;
                }
            }

            if (!IsPermanentChunks) {
                _existingScratch.Clear();
                foreach (Vector2Int c in layer.States.Keys) _existingScratch.Add(c);
                foreach (Vector2Int c in layer.Pending.Keys) _existingScratch.Add(c);

                foreach (Vector2Int coord in _existingScratch)
                    if (!_requiredScratch.Contains(coord))
                        RequestUnload(layer, coord);
            }
        }

        if (IsPermanentChunks && firstSync) {
            _initialLoadRemaining = enqueuedLoads;
            if (_initialLoadRemaining == 0) de_ChunksLoaded?.Invoke();
        }

        //VoidLatencyChecker.End("ChunksGrid.Resync");
    }

    // Single pending-task slot per (layer, coord) - load/save/unload are mutually exclusive.
    // A not-yet-started task gets canceled outright by its opposite; a running one gets
    // followed by its opposite once it finishes.
    private bool RequestLoad (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.TryGetValue(coord, out ChunkTask? pending)) {
            if (pending.Type == ChunkTaskType.Load) return false;
            if (pending.Type != ChunkTaskType.Unload) return false; // Save in flight - leave it be

            if (!pending.Started) {
                Cancel(pending); // chunk stays Ready, was never actually unloaded
                return false;
            }
            Enqueue(layer, coord, ChunkTaskType.Load); // unload already running - queue load right after
            return true;
        }

        if (layer.GetState(coord) == ChunkState.Ready) return false;
        Enqueue(layer, coord, ChunkTaskType.Load);
        return true;
    }

    private void RequestUnload (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.TryGetValue(coord, out ChunkTask? pending)) {
            if (pending.Type == ChunkTaskType.Unload) return;

            if (pending.Started) {
                Enqueue(layer, coord, ChunkTaskType.Unload); // load/save already running - queue unload right after
                return;
            }

            Cancel(pending); // not started yet, no wasted work
            if (pending.Type == ChunkTaskType.Load) return; // never actually loaded, nothing to unload
                                                            // Kind == Save: chunk is already Ready, unload will persist it - no need to save separately
        } else if (layer.GetState(coord) != ChunkState.Ready) {
            return;
        }

        Enqueue(layer, coord, ChunkTaskType.Unload);
    }

    private void Enqueue (ChunkLayer layer, Vector2Int coord, ChunkTaskType kind) {
        ChunkTask task = new ChunkTask(layer, coord, kind);
        layer.Pending[coord] = task;
        _pending.Add(task);
    }

    /// filterKind == null means "any kind" - picks the single best not-started, not-blocked task.
    /// filterKind set means "only consider tasks of this kind" - used to fill a reserved floor.
    private ChunkTask? PickNextTask (ChunkTaskType? filterKind) {
        ChunkTask? best = null;
        for (int i = 0; i < _pending.Count; i++) {
            ChunkTask t = _pending[i];
            if (t.Started) continue;
            if (filterKind != null && t.Type != filterKind) continue;
            if (IsBlocked(t)) continue;
            if (best == null || IsBetter(t, best)) best = t;
        }
        return best;
    }
    /// A chained task (Unload queued right after a running Load, etc) must wait for its
    /// predecessor on the same chunk to actually finish - otherwise both run at once.
    private bool IsBlocked (ChunkTask t) {
        if (t.Layer.Running.Contains(t.Coord)) return true; // O(1) - was O(n)

        if (t.Type == ChunkTaskType.Unload) {
            if (_dependents.TryGetValue(t.Layer, out List<ChunkLayer>? dependents))
                foreach (ChunkLayer dependent in dependents)
                    if (IsCoordActive(dependent, t.Coord)) return true;
        } else {
            if (_dependencyLayers.TryGetValue(t.Layer, out List<ChunkLayer>? deps))
                foreach (ChunkLayer dep in deps)
                    if (dep.GetState(t.Coord) != ChunkState.Ready) return true;
        }
        return false;
    }
    /// Cancels a not-yet-started task outright - must remove it from BOTH the lookup
    /// dict and the run queue, or it silently runs anyway despite being "cancelled".
    private void Cancel (ChunkTask task) {
        task.Layer.Pending.Remove(task.Coord);
        _pending.Remove(task);
    }
    // Non-unload work beats unload work; within each, live distance to the center chunk breaks
    // ties (closer first for load/save, farther first for unload, so it stays correct as Center
    // keeps moving mid-queue); dependency order breaks the rest.
    private bool IsBetter (ChunkTask a, ChunkTask b) {
        bool unloadA = a.Type == ChunkTaskType.Unload;
        bool unloadB = b.Type == ChunkTaskType.Unload;
        if (unloadA != unloadB) return !unloadA; // reverted - budgets handle fairness now, not this

        int da = ChunkDistanceSq(a.Coord), db = ChunkDistanceSq(b.Coord);
        if (da != db) return unloadA ? da > db : da < db;

        return unloadA
            ? _unloadRank[a.Layer] < _unloadRank[b.Layer]
            : _loadRank[a.Layer] < _loadRank[b.Layer];
    }

    private void TaskStart (ChunkTask task) {
        ChunkLayer layer = task.Layer;
        layer.States[task.Coord] = task.Type switch {
            ChunkTaskType.Load => ChunkState.Loading,
            ChunkTaskType.Save => ChunkState.Saving,
            ChunkTaskType.Unload => ChunkState.Unloading,
            _ => ChunkState.None,
        };
        layer.Running.Add(task.Coord);

        //string tag = $"{layer.Name}.{task.Type}";
        //VoidLatencyChecker.Start(tag);
        task.Running = task.Type switch {
            ChunkTaskType.Load => layer.RunLoad(task.Coord),
            ChunkTaskType.Save => layer.RunSave(task.Coord),
            ChunkTaskType.Unload => layer.RunUnload(task.Coord),
            _ => Task.CompletedTask,
        };
        //VoidLatencyChecker.End(tag);
    }

    private void Finish (ChunkTask task) {
        ChunkLayer layer = task.Layer;
        layer.Running.Remove(task.Coord);
        if (layer.Pending.TryGetValue(task.Coord, out ChunkTask? current) && current == task)
            layer.Pending.Remove(task.Coord);
        // else a newer task was already chained in for this coord - leave the dict pointing at it

        switch (task.Type) {
            case ChunkTaskType.Load:
                layer.States[task.Coord] = ChunkState.Ready;
                de_ChunkLoaded?.Invoke(layer, task.Coord);
                if (IsPermanentChunks && 0 < _initialLoadRemaining && --_initialLoadRemaining == 0)
                    de_ChunksLoaded?.Invoke();
                break;
            case ChunkTaskType.Save:
                layer.States[task.Coord] = ChunkState.Ready;
                break;
            case ChunkTaskType.Unload:
                layer.States.Remove(task.Coord);
                de_ChunkUnloaded?.Invoke(layer, task.Coord);
                break;
        }
    }

    // Chunk-index space distance from the current center chunk - no ChunkSize involved,
    // so Radius is always "N chunks out" regardless of chunk size.
    private int ChunkDistanceSq (Vector2Int coord) {
        Vector2Int d = coord - _lastCenterChunk;
        return d.X * d.X + d.Y * d.Y;
    }

    private bool IsInRange (Vector2Int coord, float radius) {
        Vector2Int d = coord - _lastCenterChunk;
        return IsCircle
            ? d.X * d.X + d.Y * d.Y <= radius * radius
            : MathF.Abs(d.X) <= radius && MathF.Abs(d.Y) <= radius;
    }

    private int RingCount (float radius) => (int)MathF.Ceiling(radius) + 1;

    private Vector2Int WorldToChunk (Vector3 pos) =>
        new((int)MathF.Floor(pos.X / ChunkSize), (int)MathF.Floor(pos.Z / ChunkSize));

    private static readonly Dictionary<int, Vector2Int[]> _spiralCache = new Dictionary<int, Vector2Int[]>();
    private static Vector2Int[] SpiralOffsets (int maxRing) {
        if (_spiralCache.TryGetValue(maxRing, out Vector2Int[]? cached)) return cached;

        List<Vector2Int> list = new List<Vector2Int> { Vector2Int.Zero };
        for (int ring = 1; ring <= maxRing; ring++) {
            int x = ring, z = -ring;
            for (; z < ring; z++) list.Add(new Vector2Int(x, z));
            for (; x > -ring; x--) list.Add(new Vector2Int(x, z));
            for (; z > -ring; z--) list.Add(new Vector2Int(x, z));
            for (; x < ring; x++) list.Add(new Vector2Int(x, z));
        }
        Vector2Int[] arr = list.ToArray();
        _spiralCache[maxRing] = arr;
        return arr;
    }

    private void RebuildLayerOrder () {
        List<ChunkLayer> sorted = new List<ChunkLayer>(Layers.Count);
        HashSet<Type> done = new HashSet<Type>();
        HashSet<Type> stack = new HashSet<Type>();

        void Visit (ChunkLayer layer) {
            Type t = layer.GetType();
            if (done.Contains(t)) return;
            if (!stack.Add(t))
                throw new InvalidOperationException($"Circular chunk layer dependency at {t.Name}.");

            foreach (Type depType in layer.Dependencies) {
                ChunkLayer? dep = Layers.Find(l => l.GetType() == depType);
                if (dep == null)
                    throw new InvalidOperationException(
                        $"{t.Name} depends on {depType.Name}, which is not registered on this grid.");
                Visit(dep);
            }

            stack.Remove(t);
            done.Add(t);
            sorted.Add(layer);
        }

        foreach (ChunkLayer layer in Layers)
            Visit(layer);

        _loadOrder = sorted.ToArray();
        _unloadOrder = new ChunkLayer[sorted.Count];
        _loadRank.Clear();
        _unloadRank.Clear();
        for (int i = 0; i < sorted.Count; i++) {
            _unloadOrder[i] = sorted[sorted.Count - 1 - i];
            _loadRank[sorted[i]] = i;
        }
        for (int i = 0; i < _unloadOrder.Length; i++)
            _unloadRank[_unloadOrder[i]] = i;

        RebuildDependents();
    }
    private void RebuildDependents () {
        _dependents.Clear();
        _dependencyLayers.Clear();
        foreach (ChunkLayer layer in Layers) {
            List<ChunkLayer> deps = new List<ChunkLayer>();
            foreach (Type depType in layer.Dependencies) {
                ChunkLayer? dep = Layers.Find(l => l.GetType() == depType);
                if (dep == null) continue;
                deps.Add(dep);
                if (!_dependents.TryGetValue(dep, out List<ChunkLayer>? list))
                    _dependents[dep] = list = new List<ChunkLayer>();
                list.Add(layer);
            }
            _dependencyLayers[layer] = deps;
        }
    }

    private bool IsCoordActive (ChunkLayer layer, Vector2Int coord) =>
        layer.GetState(coord) != ChunkState.None || layer.Pending.ContainsKey(coord);

}
