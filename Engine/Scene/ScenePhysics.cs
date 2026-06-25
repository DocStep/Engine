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
        GameObject plane = new GameObject() { Name = "Plane", };
        plane.Transform.Position = new Vector3(0, 0, 0);
        plane.Transform.Scale = new Vector3(10, 1f, 10);
        plane.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Plane, });
        plane.AddComponent(new BoxColliderComponent() { scale = new Vector3(1, 0.01f, 1), });


        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(0, 3, 0);
        cube.Transform.Rotation = new Vector3(30, 0, 0);
        cube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Cube, });
        cube.AddComponent(new PhysicsComponent());
        //cube.AddComponent(new BoxColliderComponent());

    }

}
