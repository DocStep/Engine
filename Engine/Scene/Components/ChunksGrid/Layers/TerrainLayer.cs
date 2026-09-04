using System.Threading.Tasks;

namespace Engine;


public sealed class TerrainLayer : ChunkLayer {
    public override string Name => "Terrain";

    public override Task RunLoad (Vector2Int coord) {
        Log.log(Name, Radius, nameof(RunLoad), coord);
        return Task.CompletedTask;
    }

    public override Task RunUnload (Vector2Int coord) {
        Log.log(Name, Radius, nameof(RunUnload), coord);
        return Task.CompletedTask;
    }
}
