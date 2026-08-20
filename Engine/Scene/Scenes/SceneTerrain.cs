using Engine.Graphics;

namespace Engine;


public class SceneTerrain : Scene {

    public override void Load () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_terrain = new GameObject() { Name = "Terrain", };
        go_terrain.Transform.Position = new Vector3(0, 0, 0);
        go_terrain.Transform.LocalScale = new Vector3(10, 1f, 10);
        MeshComponent terrain_meshComp = go_terrain.AddComponent<MeshComponent>();
        Noise noise = new Noise(0.2f);
        int count = 10;
        float[,] heightmap = new float[count, count];
        int l0 = heightmap.GetLength(0);
        int l1 = heightmap.GetLength(1);
        for (int yy = 0; yy < l0; yy++) {
            for (int xx = 0; xx < l1; xx++) {
                heightmap[xx, yy] = 2*noise.Value(xx, yy);
            }
        }
        terrain_meshComp.mesh = new Mesh(MeshData.CreateFromArray(heightmap, 1f));
        terrain_meshComp.material = new Material(AssetsEngine._mat_Lit) { Name = "Terrain", };
        terrain_meshComp.material.SetVector3(Shader.Color, Constants.white);
        terrain_meshComp.material.SetFloat(Shader.Smoothness, 0f);
        MeshColliderComponent terrain_meshColliderComp = go_terrain.AddComponent<MeshColliderComponent>();
        terrain_meshColliderComp.SetMesh(terrain_meshComp.mesh);

        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(0, 10, 0);
        cube.AddComponent<MeshComponent>().mesh = AssetsEngine._mesh_Cube;
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>();

        GameObject go_lightSources = new GameObject() { Name = "Light Sources", };

        GameObject go_sun1 = new GameObject() { Name = "Sun1", };
        go_sun1.Transform.Parent = go_lightSources.Transform;
        go_sun1.Transform.LocalPosition = new Vector3(0, 5, 0);
        go_sun1.Transform.LocalEuler = new Vector3(60, -30, 0);
        SunLight sun1 = go_sun1.AddComponent<SunLight>();
        sun1.Color = new Vector3(0, 1, 0);

        GameObject go_sun2 = new GameObject() { Name = "Sun2", };
        go_sun2.Transform.Parent = go_lightSources.Transform;
        go_sun2.Transform.LocalPosition = new Vector3(0, 5, 0);
        go_sun2.Transform.LocalEuler = new Vector3(30, 30, 0);
        SunLight sun2 = go_sun2.AddComponent<SunLight>();
        sun2.Color = new Vector3(0, 0, 1);

        GameObject go_pointLight = new GameObject() { Name = "Point Light", };
        go_pointLight.Transform.Parent = go_lightSources.Transform;
        go_pointLight.Transform.LocalPosition = new Vector3(-1, 2, -1);
        PointLight pointLight = go_pointLight.AddComponent<PointLight>();
        pointLight.Color = new Vector3(1, 0, 0);

    }

}
