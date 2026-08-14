using Engine.Graphics;

namespace Engine;


public class SceneTerrain : Scene {

    public override void Load () {
        GameObject cam = new GameObject() { Name = "Camera", };
        cam.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = cam.AddComponent<Camera>();


        Noise nosie = new Noise(0.01f);
        float[,] heightmap = new float[100, 100];
        int l0 = heightmap.GetLength(0);
        int l1 = heightmap.GetLength(1);
        for (int yy = 0; yy < l0; yy++) {
            for (int xx = 0; xx < l1; xx++) {
                heightmap[xx, yy] = nosie.Value(xx, yy);
            }
        }
        GameObject terrain = new GameObject() { Name = "Terrain", };
        terrain.Transform.Position = new Vector3(0, 1, 0);
        terrain.Transform.Scale = new Vector3(5, 0.5f, 5);
        MeshComponent terrain_meshComp = terrain.AddComponent<MeshComponent>();
        terrain_meshComp.mesh = new Mesh(MeshData.CreateFromArray(heightmap, 1f));
        terrain_meshComp.material = new Material(AssetsEngine._mat_Lit);
        terrain_meshComp.material.SetVector3(Shader.Color, Constants.green);
        terrain_meshComp.material.SetFloat(Shader.Smoothness, 0f);

    }

}
