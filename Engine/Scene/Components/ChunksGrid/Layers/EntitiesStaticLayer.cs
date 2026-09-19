using System.Threading.Tasks;
using Engine.Graphics;

namespace Engine;


public sealed class EntitiesStaticLayer : ChunkLayer {

    public override string Name => "EntitiesStatic";

    public override IReadOnlyList<Type> Dependencies { get; } = [ typeof(TerrainLayer) ];

    Material mat = new Material(AssetsEngine._sh_LitInstanced);


    public override Task RunLoad (Vector2Int coord) {
        mat.SetVector3(Shader.Color, new Vector3(0, 1, 0));
        List<GameObject> list = GetObjects(coord);
        int n = 1;
        for (int i = 0; i < n; i++) {
            Vector3 pos = ChunksGrid.ChunkSize*new Vector3(coord.X + 0.5f, 0, coord.Y + 0.5f) + Vector3.UnitY;
            GameObject go_cube = new GameObject(PrimitiveTypes.Cube, pos, scale: new Vector3(5, 1, 5)) { Name = coord.ToString(), };
            go_cube.Transform.Parent = ChunksGrid.Transform;
            go_cube.GetComponent<MeshComponent>()?.Material = mat;

            list.Add(go_cube);
        }

        return Task.CompletedTask;
    }

}
