using System.Threading.Tasks;

namespace Engine;


/// Per-layer load/save/unload hooks + own streaming radius. Layers own their data;
/// the grid only decides when to run them, in what order, and how much per tick.
/// RunLoad/RunSave/RunUnload should return quickly - offload real work internally
/// (Task.Run, your own job system, etc). ProcessTasks() only polls IsCompleted,
/// it does not isolate synchronous work for you.
public abstract class ChunkLayer {

    public abstract string Name { get; }
    public virtual IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
    public float Radius = 64f; // own radius against the grid's shared shape (IsCircle)

    public virtual Task RunLoad (Vector2Int coord) => Task.CompletedTask;
    public virtual Task RunSave (Vector2Int coord) => Task.CompletedTask;
    public virtual Task RunUnload (Vector2Int coord) => Task.CompletedTask;

    internal readonly Dictionary<Vector2Int, ChunkState> States = new();
    internal readonly Dictionary<Vector2Int, ChunkTask> Pending = new();

    public ChunkState GetState (Vector2Int coord) =>
        States.TryGetValue(coord, out var s) ? s : ChunkState.None;

}
