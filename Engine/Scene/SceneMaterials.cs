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
        MeshComponent mesh;

        x = 4;
        GameObject plane = new GameObject() { Name = "Plane", };
        plane.Transform.Position = new Vector3(x, 0, -2);
        mesh = plane.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Plane;

        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(x, 0, 0);
        mesh = cube.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Cube;

        GameObject sphere = new GameObject() { Name = "Sphere", };
        sphere.Transform.Position = new Vector3(x, 0, 2);
        mesh = sphere.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;

        GameObject capsule = new GameObject() { Name = "Capsule", };
        capsule.Transform.Position = new Vector3(x, 0, 4);
        mesh = capsule.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Capsule;

        /// Gizmos
        x = 6;
        GameObject gizmosPlane = new GameObject() { Name = "Gizmos Plane", };
        gizmosPlane.Transform.Position = new Vector3(x, 0, -2);
        mesh = gizmosPlane.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_GizmoPlane;
        mesh.material = Renderer.Instance._mat_GizmosG;
        mesh.shader = Renderer.Instance._sh_Unlit;
        mesh.primitiveType = PrimitiveType.Lines;

        GameObject gizmosCube = new GameObject() { Name = "Gizmos Cube", };
        gizmosCube.Transform.Position = new Vector3(x, 0, 0);
        mesh = gizmosCube.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_GizmoCube;
        mesh.material = Renderer.Instance._mat_GizmosG;
        mesh.shader = Renderer.Instance._sh_Unlit;
        mesh.primitiveType = PrimitiveType.Lines;

        GameObject gizmosSphere = new GameObject() { Name = "Gizmos Sphere", };
        gizmosSphere.Transform.Position = new Vector3(x, 0, 2);
        mesh = gizmosSphere.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_GizmoSphere;
        mesh.material = Renderer.Instance._mat_GizmosG;
        mesh.shader = Renderer.Instance._sh_Unlit;
        mesh.primitiveType = PrimitiveType.Lines;

        GameObject gizmosCapsule = new GameObject() { Name = "Gizmos Capsule", };
        gizmosCapsule.Transform.Position = new Vector3(x, 0, 4);
        mesh = gizmosCapsule.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_GizmoCapsule;
        mesh.material = Renderer.Instance._mat_GizmosG;
        mesh.shader = Renderer.Instance._sh_Unlit;
        mesh.primitiveType = PrimitiveType.Lines;

        x = 8;
        GameObject sphereR = new GameObject() { Name = "Sphere R", };
        sphereR.Transform.Position = new Vector3(x, 0, 0);
        mesh = sphereR.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_LitRed;

        GameObject sphereG = new GameObject() { Name = "Sphere G", };
        sphereG.Transform.Position = new Vector3(x, 0, 2);
        mesh = sphereG.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_LitGreen;

        GameObject sphereB = new GameObject() { Name = "Sphere B", };
        sphereB.Transform.Position = new Vector3(x, 0, 4);
        mesh = sphereB.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_LitBlue;

        x = 10;
        GameObject sphereMatt = new GameObject() { Name = "Sphere Matt", };
        sphereMatt.Transform.Position = new Vector3(x, 0, 0);
        mesh = sphereMatt.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_Matt;

        GameObject sphereSmooth = new GameObject() { Name = "Sphere Smooth", };
        sphereSmooth.Transform.Position = new Vector3(x, 0, 2);
        mesh = sphereSmooth.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_Smooth;

        GameObject reflectionSphere = new GameObject() { Name = "Reflection Sphere", };
        reflectionSphere.Transform.Position = new Vector3(0, 0, -8);
        reflectionSphere.Transform.Scale = 2*Vector3.One;
        mesh = reflectionSphere.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Sphere;
        mesh.material = Renderer.Instance._mat_MaterialPreview;

        /// Reflection
        x = 0;
        GameObject reflectionSuzanneHightRes = new GameObject() { Name = "Reflection SuzanneHightRes", };
        reflectionSuzanneHightRes.Transform.Position = new Vector3(x, 0, 0);
        reflectionSuzanneHightRes.Transform.Rotation = new Vector3(0, 180, 0);
        mesh = reflectionSuzanneHightRes.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_SuzanneHighRes;
        mesh.material = Renderer.Instance._mat_MaterialPreview;

        GameObject reflectionSuzanne = new GameObject() { Name = "Reflection Suzanne", };
        reflectionSuzanne.Transform.Position = new Vector3(x, 0, 4);
        reflectionSuzanne.Transform.Rotation = new Vector3(0, 180, 0);
        mesh = reflectionSuzanne.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Suzanne;
        mesh.material = Renderer.Instance._mat_MaterialPreview;

        GameObject reflectionTorus = new GameObject() { Name = "Reflection Torus", };
        reflectionTorus.Transform.Position = new Vector3(x, 0, 8);
        mesh = reflectionTorus.AddComponent<MeshComponent>();
        mesh.mesh = Renderer.Instance._mesh_Torus;
        mesh.material = Renderer.Instance._mat_MaterialPreview;

    }

}
