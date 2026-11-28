using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Engine;


/// Per-layer load/save/unload hooks + own streaming radius. Layers own their data;
/// the grid only decides when to run them, in what order, and how much per tick.
/// RunLoad/RunSave/RunUnload should return quickly - offload real work internally
/// (Task.Run, your own job system, etc). ProcessTasks() only polls IsCompleted,
/// it does not isolate synchronous work for you.
public abstract class ChunkLayer {

    public abstract string Name { get; protected set; }
    public virtual List<Type> Dependencies { get; } = [];
    public float Radius = 64f; /// own radius against the grid's shared shape (IsCircle)

    [JsonIgnore] public string ChunkObjectNamePrefix => Name + ":";

    public void OnLoaded () {
        foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(this) is IOnLoaded asset) asset.OnLoaded();
    }

    public static bool TryParseCoordinate (string value, out Vector2Int coord) {
        coord = default;
        if (value.Length < 5 || value[0] != '(' || value[^1] != ')') return false;
        string[] parts = value[1..^1].Split(',', 2);
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
            || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
            return false;
        coord = new Vector2Int(x, y);
        return true;
    }

    [JsonIgnore, Hide] public readonly HashSet<Vector2Int> Running = new();
    [JsonIgnore, Hide] public readonly Dictionary<Vector2Int, ChunkState> States = new();
    [JsonIgnore, Hide] public readonly Dictionary<Vector2Int, ChunkTask> Pending = new();

    [JsonIgnore, Hide] public readonly Dictionary<Vector2Int, List<GameObject>> GameObjects = new();


    public virtual Task RunLoad (Vector2Int coord) => Task.CompletedTask;
    public virtual Task RunSave (Vector2Int coord) => Task.CompletedTask;
    public virtual Task RunUnload (Vector2Int coord) {
        if (!GameObjects.TryGetValue(coord, out List<GameObject>? list)) {
            //Log.log(Name, nameof(RunUnload), coord, "null");
            return Task.CompletedTask;
        }

        int n = list.Count;
        for (int i = n - 1; 0 <= i; i--) {
            list[i].Destroy();
        }
        GameObjects.Remove(coord);
        //Log.log(Name, nameof(RunUnload), coord, list.Count);

        return Task.CompletedTask;
    }


    /// <summary>Gets (creating if needed) the GameObject list owned by a chunk.</summary>
    protected List<GameObject> GetObjects (Vector2Int coord) {
        if (!GameObjects.TryGetValue(coord, out List<GameObject>? list)) {
            list = new List<GameObject>();
            GameObjects[coord] = list;
        }
        return list;
    }

    public ChunkState GetState (Vector2Int coord) =>
        States.TryGetValue(coord, out ChunkState s) ? s : ChunkState.None;

}
