namespace Engine;


internal sealed class ChunkTask {
    public readonly ChunkLayer Layer;
    public readonly Vector2Int Coord;
    public readonly ChunkTaskKind Kind;
    public System.Threading.Tasks.Task? Running;
    public bool Started => Running != null;

    public ChunkTask (ChunkLayer layer, Vector2Int coord, ChunkTaskKind kind) {
        Layer = layer;
        Coord = coord;
        Kind = kind;
    }
}
