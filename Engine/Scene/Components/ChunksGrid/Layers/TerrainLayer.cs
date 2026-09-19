using System.Threading.Tasks;

namespace Engine;


public sealed class TerrainLayer : ChunkLayer {

    public override string Name => "Terrain";


    public override Task RunLoad (Vector2Int coord) {
        List<GameObject> list = GetObjects(coord);
        int n = 1;
        for (int i = 0; i < n; i++) {
            Vector3 pos = ChunksGrid.ChunkSize*new Vector3(coord.X + 0.5f, 0, coord.Y + 0.5f);
            GameObject go_cube = new GameObject(PrimitiveTypes.Cube, pos,
                scale: new Vector3(0.5f*ChunksGrid.ChunkSize, 1, 0.5f*ChunksGrid.ChunkSize)) { Name = coord.ToString(), };
            //GameObject go_cube = new GameObject() { Name = coord.ToString() };
            go_cube.Transform.Parent = ChunksGrid.Transform;
            list.Add(go_cube);
        }

        //Log.log(Name, nameof(RunLoad), coord, list.Count);
        return Task.CompletedTask;
    }

}
