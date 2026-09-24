using System.Threading.Tasks;
using Engine.Graphics;

namespace Engine;


public sealed class TerrainLayer : ChunkLayer {

    public override string Name { get; protected set; } = "Terrain";

    Material mat = null!;

    public override Task RunLoad (Vector2Int chunkPos2Int) {
        List<GameObject> list = GetObjects(chunkPos2Int);
        //int n = 1;
        //for (int i = 0; i < n; i++) {
        //    Vector3 pos = ChunksGrid.ChunkSize*new Vector3(coord.X + 0.5f, 0, coord.Y + 0.5f);
        //    GameObject go_cube = new GameObject(PrimitiveTypes.Cube, pos,
        //        scale: new Vector3(0.5f*ChunksGrid.ChunkSize, 1, 0.5f*ChunksGrid.ChunkSize)) { Name = coord.ToString(), };
        //    go_cube.Transform.Parent = ChunksGrid.Transform;
        //    list.Add(go_cube);
        //}

        mat = new Material(AssetsEngine._mat_Lit);
        mat.SetVector3(Shader.Color, new Vector3(0.1f, 0.75f, 0));
        mat.SetFloat(Shader.Smoothness, 0.2f);

        Vector3 pos = ChunksGrid.ChunkSize*new Vector3(chunkPos2Int.X + 0.5f, 0, chunkPos2Int.Y + 0.5f);
        GameObject go_terrain = new GameObject() { Name = chunkPos2Int.ToString() };
        go_terrain.Transform.Position = pos;
        //go_cube.Transform.LocalScale = new Vector3(0.5f*ChunksGrid.ChunkSize, 1, 0.5f*ChunksGrid.ChunkSize);
        go_terrain.Transform.Parent = ChunksGrid.Transform;
        MeshComponent go_mesh = go_terrain.AddComponent<MeshComponent>();
        
        int chunkHeightmapResolution = ChunksGrid.ChunkSize;
        int res = chunkHeightmapResolution+1;
        float[,] heightmap = new float[res, res];
        for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++) {
                heightmap[x, y] = HeightGenerateCustom_Task(PosInner_pos2(chunkPos2Int, new Vector2(x, y)));
                //heightmap[x, y] = 0;
            }
        go_mesh.Mesh = new Mesh(MeshData.FromHeightmap(heightmap, ChunksGrid.ChunkSize));
        go_mesh.Material = mat;
        go_terrain.AddComponent<MeshColliderComponent>().SetMesh(go_mesh.Mesh);
        list.Add(go_terrain);

        GameObject go_cube = new GameObject(PrimitiveTypes.Cube, pos + 2*Vector3.UnitY);
        go_cube.Transform.Parent = ChunksGrid.Transform;
        go_cube.AddComponent<BoxColliderComponent>();
        go_cube.AddComponent<PhysicsComponent>();
        list.Add(go_cube);

        return Task.CompletedTask;


        Vector2 PosInner_pos2 (Vector2Int Pos, Vector2 posInner) {
            return ChunksGrid.ChunkSize*new Vector2(Pos.X + posInner.X/(chunkHeightmapResolution),
                Pos.Y + posInner.Y/(chunkHeightmapResolution));
        }
    }

    Noise noise = new Noise(0.01f);
    public float HeightGenerateCustom_Task (Vector2 pos) {
        return 5*noise.Value(pos);
    }

}
