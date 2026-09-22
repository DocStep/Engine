using Engine.Graphics;
using Engine.Graphics.UI;

namespace Engine;


public class SceneChunksGrid : Scene {

    public override void OnCreate () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_sun = new GameObject() { Name = "Sun", };
        go_sun.Transform.RotationEuler = new Vector3(60, -30, 0);
        SunLight sun = go_sun.AddComponent<SunLight>();

        GameObject go_grid;

        //go_grid = new GameObject() { Name = "Chunks Grid", };
        //ChunksGrid grid = go_grid.AddComponent<ChunksGrid>();
        //new GameObject() { Name = "inner", }.Transform.Parent = grid.gameObject.Transform;
        //grid.IsPermanentChunks = true;
        //grid.AddLayer(new TerrainLayer() { Radius = 2 });
        //grid.AddLayer(new EntitiesStaticLayer() { Radius = 2 });
        //grid.AddLayer(new EntitiesLayer() { Radius = 1 });
        //grid.gameObject.Save("src/Prefabs/chunksgrid.json");

        go_grid = Assets.Load<GameObject>("src/Prefabs/chunksgrid.json");

        //Log.log(go_grid.Name);

        //GameObject? go;

        //go = new GameObject() { Name = "Prefab", };
        //GameObject go_mesh = new GameObject(PrimitiveTypes.Cube) { Name = "Mesh", };
        //go_mesh.Transform.Parent = go.Transform;
        //MonkeyScript script = go_mesh.AddComponent<MonkeyScript>();
        //script.dir = new Vector3(90, 90, 0);
        //script.tr1 = go.Transform;
        //script.tr2 = go_mesh.Transform;
        //go.Save("src/Prefabs/Prefab.json");

        //go = Assets.Load<GameObject>("src/Prefabs/Prefab.json");

        //Log.log(go.Name);
    }
    
}
