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
        GameObject ground = new GameObject() { Name = "Plane", };
        ground.Transform.Position = new Vector3(0, 0, 0);
        ground.Transform.Scale = new Vector3(10, 1f, 10);
        ground.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Cube, });
        ground.AddComponent(new BoxColliderComponent() { scale = new Vector3(1, 1f, 1), isStatic = true, });
        ground.AddComponent(new PhysicsComponent() { isKinematic = true, });


        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(0, 10, 0);
        cube.Transform.Rotation = new Vector3(45, 0, 0);
        cube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Cube, });
        cube.AddComponent(new BoxColliderComponent());
        cube.AddComponent(new PhysicsComponent());
        //cube.AddComponent(new BoxColliderComponent());

    }

}
