namespace Engine;


public sealed class ChunkTask {
    public ChunkTask (ChunkLayer layer, Vector2Int coord, ChunkTaskType type) {
        Layer = layer;
        Coord = coord;
        Type = type;
    }

    public readonly ChunkLayer Layer;
    public readonly Vector2Int Coord;
    public readonly ChunkTaskType Type;
    public System.Threading.Tasks.Task? Running;
    public bool Started => Running != null;

}
