using System.Threading.Tasks;

namespace Engine;


public sealed class EntitiesLayer : ChunkLayer {
    public override string Name => "Entities";
    public override IReadOnlyList<Type> Dependencies { get; } = new[] { typeof(TerrainLayer) };

    public EntitiesLayer () { Radius = 32f; } // stays closer to the player than terrain

    public override Task RunLoad (Vector2Int coord) {
        // spawn simulated entities for this chunk (terrain is guaranteed loaded first)
        return Task.CompletedTask;
    }

    public override Task RunUnload (Vector2Int coord) {
        // despawn/save entities
        return Task.CompletedTask;
    }
}
