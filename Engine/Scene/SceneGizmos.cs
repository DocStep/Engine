namespace Engine;


public class SceneGizmos : Scene {
    public SceneGizmos () {

    }

    public override void Load () {
        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = Vector3.One;
        cube.Transform.Scale = 0.5f*Vector3.One;
        cube.AddComponent<Graphics.MeshComponent>().mesh = Graphics.Renderer.Instance._mesh_Cube;
    }

}
