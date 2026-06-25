using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public class ScenePhysics : Scene {
    public ScenePhysics () {

    }

    public override void Load () {
        GameObject plane = new GameObject() { Name = "Cube", };
        plane.Transform.position = new Vector3(0, 0, 0);
        plane.Transform.scale = new Vector3(10, 1f, 10);
        plane.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Plane, });
        plane.AddComponent(new BoxColliderComponent() { scale = new Vector3(1, 0.01f, 1), });




    }

}
