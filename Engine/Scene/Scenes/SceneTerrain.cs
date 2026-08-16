using Engine.Graphics;

namespace Engine;


public class SceneTerrain : Scene {

    public override void Load () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_sun = new GameObject() { Name = "Sun", };
        go_sun.Transform.Position = new Vector3(0, 5, 0);
        go_sun.Transform.Scale = new Vector3(0.2f);
        MeshComponent sunMesh = go_sun.AddComponent<MeshComponent>();
        sunMesh.mesh = AssetsEngine._mesh_Sphere;
        Material mat_sun = new Material(AssetsEngine._mat_Unlit);
        mat_sun.pass = RenderPass.Transparent;
        mat_sun.SetFloat(Shader.Alpha, 0.5f);
        mat_sun.SetVector3(Shader.Color, Constants.yellow);
        sunMesh.material = mat_sun;
        SunLight sun = go_sun.AddComponent<SunLight>();

        GameObject go_terrain = new GameObject() { Name = "Terrain", };
        go_terrain.Transform.Position = new Vector3(0, 0, 0);
        go_terrain.Transform.Scale = new Vector3(10, 1f, 10);
        MeshComponent terrain_meshComp = go_terrain.AddComponent<MeshComponent>();
        Noise nosie = new Noise(0.2f);
        int count = 5;
        float[,] heightmap = new float[count, count];
        int l0 = heightmap.GetLength(0);
        int l1 = heightmap.GetLength(1);
        for (int yy = 0; yy < l0; yy++) {
            for (int xx = 0; xx < l1; xx++) {
                heightmap[xx, yy] = nosie.Value(xx, yy);
            }
        }
        terrain_meshComp.mesh = new Mesh(MeshData.CreateFromArray(heightmap, 1f));
        terrain_meshComp.material = new Material(AssetsEngine._mat_Lit) { Name = "Terrain", };
        terrain_meshComp.material.SetVector3(Shader.Color, Constants.green);
        terrain_meshComp.material.SetFloat(Shader.Smoothness, 0f);
        MeshColliderComponent terrain_meshColliderComp = go_terrain.AddComponent<MeshColliderComponent>();
        terrain_meshColliderComp.SetMesh(terrain_meshComp.mesh);


        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(0, 10, 0);
        //cube.Transform.Rotation = new Vector3(30, 0, 0);
        cube.AddComponent<MeshComponent>().mesh = AssetsEngine._mesh_Cube;
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>().SetDynamic();
    }

}
