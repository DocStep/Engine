using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public class SceneGizmos : Scene {
    public SceneGizmos () {

    }

    public override void Load () {
        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = Vector3.One;
        cube.Transform.Scale = 0.5f*Vector3.One;
        cube.AddComponent<MeshComponent>().mesh = Renderer.Instance._mesh_Cube;
    }

}
