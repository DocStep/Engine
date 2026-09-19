using System.Threading.Tasks;
using Engine.Graphics;

namespace Engine;


public sealed class EntitiesLayer : ChunkLayer {
    public EntitiesLayer () {
        Radius = 32f;
    }


    public override string Name => "Entities";

    public override IReadOnlyList<Type> Dependencies { get; } = new[] { typeof(TerrainLayer) };

    Material mat = new Material(AssetsEngine._sh_LitInstanced);


    public override Task RunLoad (Vector2Int coord) {
        mat.SetVector3(Shader.Color, new Vector3(1, 0, 0));

        List<GameObject> list = GetObjects(coord);
        int n = 1;
        for (int i = 0; i < n; i++) {
            Vector3 pos = ChunksGrid.ChunkSize*new Vector3(coord.X + 0.5f, 0, coord.Y + 0.5f) + 2*Vector3.UnitY;
            GameObject go_cube = new GameObject(PrimitiveTypes.Cube, pos, scale: new Vector3(2, 2, 2)) { Name = coord.ToString(), };
            go_cube.Transform.Parent = ChunksGrid.Transform;
            go_cube.GetComponent<MeshComponent>()?.Material = mat;

            list.Add(go_cube);
        }

        return Task.CompletedTask;
    }

}
