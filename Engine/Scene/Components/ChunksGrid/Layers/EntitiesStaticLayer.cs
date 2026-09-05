using System.Threading.Tasks;

namespace Engine;


public sealed class EntitiesStaticLayer : ChunkLayer {

    public override string Name => "EntitiesStatic";

    public override IReadOnlyList<Type> Dependencies { get; } = new[] { typeof(TerrainLayer) };
    public EntitiesStaticLayer (float terrainRadius) {
        Radius = terrainRadius;
    }


    public override Task RunLoad (Vector2Int coord) {
        return Task.CompletedTask;
    }
    public override Task RunUnload (Vector2Int coord) {
        return Task.CompletedTask;
    }

}
