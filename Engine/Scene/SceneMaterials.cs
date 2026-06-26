using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public class SceneMaterials : Scene {
    public SceneMaterials () {

    }

    public override void Load () {
        float x = 0;

        x = 4;
        GameObject Plane = new GameObject() { Name = "Plane", };
        Plane.Transform.Position = new Vector3(x, 0, -2);
        Plane.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Plane, });

        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(x, 0, 0);
        cube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Cube, });

        GameObject sphere = new GameObject() { Name = "Sphere", };
        sphere.Transform.Position = new Vector3(x, 0, 2);
        sphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, });
        sphere.AddComponent(new BoxColliderComponent());

        GameObject Capsule = new GameObject() { Name = "Capsule", };
        Capsule.Transform.Position = new Vector3(x, 0, 4);
        Capsule.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Capsule, });
        
        /// Gizmos
        x = 6;
        GameObject gizmosPlane = new GameObject() { Name = "Gizmos Plane", };
        gizmosPlane.Transform.Position = new Vector3(x, 0, -2);
        gizmosPlane.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoPlane, material = Renderer.Instance._mat_GizmosG,
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        GameObject gizmosCube = new GameObject() { Name = "Gizmos Cube", };
        gizmosCube.Transform.Position = new Vector3(x, 0, 0);
        gizmosCube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoCube, material = Renderer.Instance._mat_GizmosG,
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        GameObject gizmosSphere = new GameObject() { Name = "Gizmos Sphere", };
        gizmosSphere.Transform.Position = new Vector3(x, 0, 2);
        gizmosSphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoSphere, material = Renderer.Instance._mat_GizmosG,
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        GameObject gizmosCapsule = new GameObject() { Name = "Gizmos Capsule", };
        gizmosCapsule.Transform.Position = new Vector3(x, 0, 4);
        gizmosCapsule.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoCapsule, material = Renderer.Instance._mat_GizmosG, 
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        x = 8;
        GameObject sphereR = new GameObject() { Name = "Sphere R", };
        sphereR.Transform.Position = new Vector3(x, 0, 0);
        sphereR.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_LitRed, });

        GameObject sphereG = new GameObject() { Name = "Sphere G", };
        sphereG.Transform.Position = new Vector3(x, 0, 2);
        sphereG.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_LitGreen, });

        GameObject sphereB = new GameObject() { Name = "Sphere B", };
        sphereB.Transform.Position = new Vector3(x, 0, 4);
        sphereB.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_LitBlue, });
        
        x = 10;
        GameObject sphereMatt = new GameObject() { Name = "Sphere Matt", };
        sphereMatt.Transform.Position = new Vector3(x, 0, 0);
        sphereMatt.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_Matt, });

        GameObject sphereSmooth = new GameObject() { Name = "Sphere Smooth", };
        sphereSmooth.Transform.Position = new Vector3(x, 0, 2);
        sphereSmooth.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_Smooth, });

        GameObject reflectionSphere = new GameObject() { Name = "Reflection Sphere", };
        reflectionSphere.Transform.Position = new Vector3(0, 0, -8);
        reflectionSphere.Transform.Scale = 2*Vector3.One;
        reflectionSphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._mat_MaterialPreview, });

        /// Reflection
        x = 0;
        GameObject reflectionSuzanneHightRes = new GameObject() { Name = "Reflection SuzanneHightRes", };
        reflectionSuzanneHightRes.Transform.Position = new Vector3(x, 0, 0);
        reflectionSuzanneHightRes.Transform.Rotation = new Vector3(0, 180, 0);
        reflectionSuzanneHightRes.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_SuzanneHighRes, material = Renderer.Instance._mat_MaterialPreview, });

        GameObject reflectionSuzanne = new GameObject() { Name = "Reflection Suzanne", };
        reflectionSuzanne.Transform.Position = new Vector3(x, 0, 4);
        reflectionSuzanne.Transform.Rotation = new Vector3(0, 180, 0);
        reflectionSuzanne.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Suzanne, material = Renderer.Instance._mat_MaterialPreview, });

        GameObject reflectionTorus = new GameObject() { Name = "Reflection Torus", };
        reflectionTorus.Transform.Position = new Vector3(x, 0, 8);
        reflectionTorus.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Torus, material = Renderer.Instance._mat_MaterialPreview, });

    }

}
