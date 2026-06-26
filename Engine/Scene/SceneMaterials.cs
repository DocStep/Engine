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
        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(-4, 0, 0);
        cube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Cube, });

        GameObject sphere = new GameObject() { Name = "Sphere", };
        sphere.Transform.Position = new Vector3(-4, 0, 2);
        sphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, });
        sphere.AddComponent(new BoxColliderComponent());

        GameObject sphereR = new GameObject() { Name = "Sphere R", };
        sphereR.Transform.Position = new Vector3(-6, 0, 0);
        sphereR.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_LitRed, });

        GameObject sphereG = new GameObject() { Name = "Sphere G", };
        sphereG.Transform.Position = new Vector3(-6, 0, 2);
        sphereG.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_LitGreen, });

        GameObject sphereB = new GameObject() { Name = "Sphere B", };
        sphereB.Transform.Position = new Vector3(-6, 0, 4);
        sphereB.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_LitBlue, });

        GameObject sphereMatt = new GameObject() { Name = "Sphere Matt", };
        sphereMatt.Transform.Position = new Vector3(-6, 0, 4);
        sphereMatt.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_Matt, });

        GameObject sphereSmooth = new GameObject() { Name = "Sphere Smooth", };
        sphereSmooth.Transform.Position = new Vector3(-6, 0, 4);
        sphereSmooth.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_Smooth, });

        GameObject reflectionSphere = new GameObject() { Name = "Reflection Sphere", };
        reflectionSphere.Transform.Position = new Vector3(0, 0, -8);
        reflectionSphere.Transform.Scale = 2*Vector3.One;
        reflectionSphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Sphere, material = Renderer.Instance._m_MaterialPreview, });

        GameObject reflectionSuzanneHightRes = new GameObject() { Name = "Reflection SuzanneHightRes", };
        reflectionSuzanneHightRes.Transform.Position = new Vector3(0, 0, 0);
        reflectionSuzanneHightRes.Transform.Rotation = new Vector3(0, 180, 0);
        reflectionSuzanneHightRes.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_SuzanneHighRes, material = Renderer.Instance._m_MaterialPreview, });

        GameObject reflectionSuzanne = new GameObject() { Name = "Reflection Suzanne", };
        reflectionSuzanne.Transform.Position = new Vector3(0, 0, 4);
        reflectionSuzanne.Transform.Rotation = new Vector3(0, 180, 0);
        reflectionSuzanne.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Suzanne, material = Renderer.Instance._m_MaterialPreview, });

        GameObject reflectionTorus = new GameObject() { Name = "Reflection Torus", };
        reflectionTorus.Transform.Position = new Vector3(0, 0, 8);
        reflectionTorus.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_Torus, material = Renderer.Instance._m_MaterialPreview, });

        /// Gizmos
        GameObject gizmosCube = new GameObject() { Name = "Gizmos Cube", };
        gizmosCube.Transform.Position = new Vector3(4, 0, 0);
        gizmosCube.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoCube, material = Renderer.Instance._mat_GizmosG,
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        GameObject gizmosSphere = new GameObject() { Name = "Gizmos Sphere", };
        gizmosSphere.Transform.Position = new Vector3(4, 0, 2);
        gizmosSphere.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoSphere, material = Renderer.Instance._mat_GizmosG,
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

        GameObject gizmosCapsule = new GameObject() { Name = "Gizmos Capsule", };
        gizmosCapsule.Transform.Position = new Vector3(4, 0, 4);
        gizmosCapsule.AddComponent(new MeshComponent() { mesh = Renderer.Instance._mesh_GizmoCapsule, material = Renderer.Instance._mat_GizmosG, 
            shader = Renderer.Instance._sh_Unlit, primitiveType = PrimitiveType.Lines, });

    }

    private readonly List<GameObject> objects = new();
    public void ObjectAdd (GameObject gameObject) {
        objects.Add(gameObject);
    }
    public void ObjectRemove (GameObject gameObject) {
        objects.Remove(gameObject);
    }

    public void Update () {

    }


    public void Destroy (GameObject go) {
        objects.Remove(go);
    }

}
