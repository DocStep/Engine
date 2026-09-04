/// 2D chunk grid (Minecraft-style column loading). Each layer streams on its own radius
/// against a shared shape (IsCircle); load/save/unload run as cancelable, budgeted, priority-
/// ordered tasks so heavy work (gen, IO) never has to block a frame - see ProcessTasks().
/// 3D later = swap Vector2Int for a 3-axis coord in OffsetFromCenter/WorldToChunk/SpiralOffsets,
/// the task/dependency/priority machinery doesn't change.

using System.Threading.Tasks;

namespace Engine;

public enum ChunkState { None, Loading, Ready, Saving, Unloading }
public enum ChunkTaskKind { Load, Save, Unload }


public sealed class ChunkGrid : Component, IUpdate {

    public override string Name => nameof(ChunkGrid);

    /// Config
    public bool IsPermanentChunks = false; /// true = load full extent once per layer, never streams/unloads on move
    public bool IsCircle = true;          /// false = quad
    public Vector3 Center = Vector3.Zero;
    public int ChunkSize = 16;
    public int MaxTasksStartedPerTick = 4; /// budget - call ProcessTasks() once per frame

    /// Events
    public event Action<ChunkLayer, Vector2Int>? de_ChunkLoaded;
    public event Action<ChunkLayer, Vector2Int>? de_ChunkUnloaded;
    public event Action? de_ChunksLoaded;   /// permanent mode only - fires once every layer's initial load finishes
    public event Action? de_ChunksUnloaded; /// UnloadAll

    private readonly List<ChunkLayer> _layers = new List<ChunkLayer>();
    private ChunkLayer[] _loadOrder = Array.Empty<ChunkLayer>();
    private ChunkLayer[] _unloadOrder = Array.Empty<ChunkLayer>();
    private readonly Dictionary<ChunkLayer, int> _loadRank = new Dictionary<ChunkLayer, int>();
    private readonly Dictionary<ChunkLayer, int> _unloadRank = new Dictionary<ChunkLayer, int>();

    private readonly List<ChunkTask> _pending = new List<ChunkTask>();
    private Vector2Int _lastCenterChunk;
    private bool _initialized;
    private int _initialLoadRemaining;

    /// Register layers before the first UpdateCenter call.
    public void AddLayer (ChunkLayer layer) {
        _layers.Add(layer);
        RebuildLayerOrder();
    }

    public void Update () {
        UpdateCenter(gameObject.Transform.Position);
        ProcessTasks();
    }

    /// Call on spawn and whenever the streaming center moves (player position, etc).
    /// Only enqueues work - pair with ProcessTasks() every frame or nothing actually runs.
    public void UpdateCenter (Vector3 newCenter) {
        Center = newCenter;

        if (IsPermanentChunks && _initialized) return;

        Vector2Int centerChunk = WorldToChunk(newCenter);
        if (_initialized && centerChunk.Equals(_lastCenterChunk)) return;

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
        for (int n = 0; n < MaxTasksStartedPerTick; n++) {
            ChunkTask? task = PickNextTask();
            if (task == null) break;
            Start(task);
        }

        for (int i = _pending.Count - 1; i >= 0; i--) {
            ChunkTask t = _pending[i];
            if (t.Started && t.Running!.IsCompleted) {
                Finish(t);
                _pending.RemoveAt(i);
            }
        }
    }

    /// Queues a save for a currently-loaded chunk. No-op if a load/unload is already pending.
    public void RequestSave (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.ContainsKey(coord)) return;
        if (layer.GetState(coord) != ChunkState.Ready) return;
        Enqueue(layer, coord, ChunkTaskKind.Save);
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
        int enqueuedLoads = 0;

        foreach (ChunkLayer layer in _loadOrder) {
            HashSet<Vector2Int> required = new HashSet<Vector2Int>();
            foreach (Vector2Int offset in SpiralOffsets(RingCount(layer.Radius))) {
                Vector2Int coord = _lastCenterChunk + offset;
                if (IsInRange(coord, layer.Radius)) {
                    required.Add(coord);
                    if (RequestLoad(layer, coord)) enqueuedLoads++;
                }
            }

            if (!IsPermanentChunks) {
                foreach (Vector2Int coord in new List<Vector2Int>(layer.States.Keys))
                    if (!required.Contains(coord))
                        RequestUnload(layer, coord);
            }
        }

        if (IsPermanentChunks && firstSync) {
            _initialLoadRemaining = enqueuedLoads;
            if (_initialLoadRemaining == 0) de_ChunksLoaded?.Invoke();
        }
    }

    // Single pending-task slot per (layer, coord) - load/save/unload are mutually exclusive.
    // A not-yet-started task gets canceled outright by its opposite; a running one gets
    // followed by its opposite once it finishes.
    private bool RequestLoad (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.TryGetValue(coord, out ChunkTask? pending)) {
            if (pending.Kind == ChunkTaskKind.Load) return false;
            if (pending.Kind != ChunkTaskKind.Unload) return false; // Save in flight - leave it be

            if (!pending.Started) {
                layer.Pending.Remove(coord); // cancel - chunk stays Ready, was never actually unloaded
                return false;
            }
            Enqueue(layer, coord, ChunkTaskKind.Load); // unload already running - queue load right after
            return true;
        }

        if (layer.GetState(coord) == ChunkState.Ready) return false;
        Enqueue(layer, coord, ChunkTaskKind.Load);
        return true;
    }

    private void RequestUnload (ChunkLayer layer, Vector2Int coord) {
        if (layer.Pending.TryGetValue(coord, out ChunkTask? pending)) {
            if (pending.Kind == ChunkTaskKind.Unload) return;
            if (pending.Kind == ChunkTaskKind.Save) return; // let the save finish, re-checked next sync

            if (!pending.Started) {
                layer.Pending.Remove(coord); // cancel - never actually loaded
                return;
            }
            Enqueue(layer, coord, ChunkTaskKind.Unload); // load already running - queue unload right after
            return;
        }

        if (layer.GetState(coord) != ChunkState.Ready) return;
        Enqueue(layer, coord, ChunkTaskKind.Unload);
    }

    private void Enqueue (ChunkLayer layer, Vector2Int coord, ChunkTaskKind kind) {
        ChunkTask task = new ChunkTask(layer, coord, kind);
        layer.Pending[coord] = task;
        _pending.Add(task);
    }

    private ChunkTask? PickNextTask () {
        ChunkTask? best = null;
        for (int i = 0; i < _pending.Count; i++) {
            ChunkTask t = _pending[i];
            if (t.Started) continue;
            if (best == null || IsBetter(t, best)) best = t;
        }
        return best;
    }

    // Non-unload work beats unload work; within each, live distance to the center chunk breaks
    // ties (closer first for load/save, farther first for unload, so it stays correct as Center
    // keeps moving mid-queue); dependency order breaks the rest.
    private bool IsBetter (ChunkTask a, ChunkTask b) {
        bool unloadA = a.Kind == ChunkTaskKind.Unload;
        bool unloadB = b.Kind == ChunkTaskKind.Unload;
        if (unloadA != unloadB) return !unloadA;

        int da = ChunkDistanceSq(a.Coord), db = ChunkDistanceSq(b.Coord);
        if (da != db) return unloadA ? da > db : da < db;

        return unloadA
            ? _unloadRank[a.Layer] < _unloadRank[b.Layer]
            : _loadRank[a.Layer] < _loadRank[b.Layer];
    }

    private void Start (ChunkTask task) {
        ChunkLayer layer = task.Layer;
        layer.States[task.Coord] = task.Kind switch {
            ChunkTaskKind.Load => ChunkState.Loading,
            ChunkTaskKind.Save => ChunkState.Saving,
            ChunkTaskKind.Unload => ChunkState.Unloading,
            _ => ChunkState.None,
        };

        task.Running = task.Kind switch {
            ChunkTaskKind.Load => layer.RunLoad(task.Coord),
            ChunkTaskKind.Save => layer.RunSave(task.Coord),
            ChunkTaskKind.Unload => layer.RunUnload(task.Coord),
            _ => Task.CompletedTask,
        };
    }

    private void Finish (ChunkTask task) {
        ChunkLayer layer = task.Layer;
        layer.Pending.Remove(task.Coord);

        switch (task.Kind) {
            case ChunkTaskKind.Load:
                layer.States[task.Coord] = ChunkState.Ready;
                de_ChunkLoaded?.Invoke(layer, task.Coord);
                if (IsPermanentChunks && 0 < _initialLoadRemaining && --_initialLoadRemaining == 0)
                    de_ChunksLoaded?.Invoke();
                break;
            case ChunkTaskKind.Save:
                layer.States[task.Coord] = ChunkState.Ready;
                break;
            case ChunkTaskKind.Unload:
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

    // Ring-by-ring square spiral around (0,0), closest ring first - seeds discovery in a
    // sensible order. Final task order is still governed by IsBetter() above.
    private static IEnumerable<Vector2Int> SpiralOffsets (int maxRing) {
        yield return new Vector2Int(0, 0);

        for (int ring = 1; ring <= maxRing; ring++) {
            int x = ring, z = -ring;
            for (; z < ring; z++) yield return new Vector2Int(x, z);
            for (; x > -ring; x--) yield return new Vector2Int(x, z);
            for (; z > -ring; z--) yield return new Vector2Int(x, z);
            for (; x < ring; x++) yield return new Vector2Int(x, z);
        }
    }

    private void RebuildLayerOrder () {
        List<ChunkLayer> sorted = new List<ChunkLayer>(_layers.Count);
        HashSet<Type> done = new HashSet<Type>();
        HashSet<Type> stack = new HashSet<Type>();

        void Visit (ChunkLayer layer) {
            Type t = layer.GetType();
            if (done.Contains(t)) return;
            if (!stack.Add(t))
                throw new InvalidOperationException($"Circular chunk layer dependency at {t.Name}.");

            foreach (Type depType in layer.Dependencies) {
                ChunkLayer? dep = _layers.Find(l => l.GetType() == depType);
                if (dep == null)
                    throw new InvalidOperationException(
                        $"{t.Name} depends on {depType.Name}, which is not registered on this grid.");
                Visit(dep);
            }

            stack.Remove(t);
            done.Add(t);
            sorted.Add(layer);
        }

        foreach (ChunkLayer layer in _layers)
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
    }

}
