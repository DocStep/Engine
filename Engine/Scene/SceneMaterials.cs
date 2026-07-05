using Engine.Graphics;

namespace Engine;


public class SceneMaterials : Scene {
    public SceneMaterials () {

    }

    public override void Load () {
        float x = 0;
        MeshComponent? mesh;

        x = 4;
        GameObject plane = new GameObject(PrimitiveTypes.Plane, position: new(x, 0, -2)) { Name = "Plane", };
        plane.RemoveComponent<PhysicsComponent>();

        GameObject cube = new GameObject(PrimitiveTypes.Cube, position: new(x, 0, 0)) { Name = "Cube", };
        cube.RemoveComponent<PhysicsComponent>();

        GameObject sphere = new GameObject(PrimitiveTypes.Sphere, position: new(x, 0, 2)) { Name = "Sphere", };
        sphere.RemoveComponent<PhysicsComponent>();

        GameObject capsule = new GameObject(PrimitiveTypes.Capsule, position: new(x, 0, 4)) { Name = "Capsule", };
        capsule.RemoveComponent<PhysicsComponent>();

        /// Gizmos
        x = 6;
        GameObject gizmosPlane = new GameObject(PrimitiveTypes.GizmoPlane, position: new(x, 0, -2)) { Name = "Gizmos Plane", };
        GameObject gizmosCube = new GameObject(PrimitiveTypes.GizmoCube, position: new(x, 0, 0)) { Name = "Gizmos Cube", };
        GameObject gizmosSphere = new GameObject(PrimitiveTypes.GizmoSphere, position: new(x, 0, 2)) { Name = "Gizmos Sphere", };
        GameObject gizmosCapsule = new GameObject(PrimitiveTypes.GizmoCapsule, position: new(x, 0, 4)) { Name = "Gizmos Capsule", };

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

    public override void DrawRaw () {
        //Renderer.Instance.DrawMaterialsGrid(0, -10, Constants.materialsGridCount, Constants.materialsGridDensity);
    }

}
