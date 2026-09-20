using Engine.Graphics;
using Engine.Graphics.UI;

namespace Engine;


public class SceneChunksGrid : Scene {

    public override void Load () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_sun = new GameObject() { Name = "Sun", };
        go_sun.Transform.RotationEuler = new Vector3(60, -30, 0);
        SunLight sun = go_sun.AddComponent<SunLight>();

        GameObject go_grid = new GameObject() { Name = "Chunks Grid", };
        ChunksGrid grid = go_grid.AddComponent<ChunksGrid>();
        grid.AddLayer(new TerrainLayer() { Radius = 10 });
        grid.AddLayer(new EntitiesStaticLayer() { Radius = 7 });
        grid.AddLayer(new EntitiesLayer() { Radius = 3 });

    }
    
}
