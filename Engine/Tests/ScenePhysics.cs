namespace Engine;


public class ScenePhysics : Scene {

    public override void OnGenerate () {
        GameObject go_sun1 = new GameObject() { Name = "Sun", };
        go_sun1.Transform.LocalPosition = new Vector3(0, 5, 0);
        go_sun1.Transform.LocalEuler = new Vector3(60, -30, 0);
        Graphics.SunLight sun1 = go_sun1.AddComponent<Graphics.SunLight>();

        //GameObject ground = new GameObject() { Name = "Plane", };
        //ground.Transform.Position = new Vector3(0, 0, 0);
        ////ground.Transform.RotationEuler = new Vector3(180, 0, 0);
        //ground.Transform.LocalScale = new Vector3(10, 1f, 10);
        //ground.AddComponent<Graphics.MeshComponent>().Mesh = AssetsEngine._mesh_PlaneQuad;
        //ground.AddComponent<PlaneColliderComponent>();
        //ground.AddComponent<PhysicsComponent>().SetKinematic();

        GameObject cube = new GameObject(PrimitiveTypes.Cube) { Name = "Cube 1", };
        cube.Transform.Position = new Vector3(0, 10, 0);
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>().SetDynamic();

        cube = new GameObject(PrimitiveTypes.Cube) { Name = "Cube 2", };
        cube.Transform.Position = new Vector3(0, 15, 0);
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>().SetDynamic();

        cube = new GameObject(PrimitiveTypes.Cube) { Name = "Cube Ground", };
        cube.Transform.LocalScale = new Vector3(10, 1, 10);
        cube.AddComponent<BoxColliderComponent>();
        //cube.AddComponent<PhysicsComponent>();
        cube.AddComponent<PhysicsComponent>().SetKinematic();

        GameObject sphere = new GameObject(PrimitiveTypes.Sphere) { Name = "Sphere", };
        sphere.Transform.Position = new Vector3(2, 10, 0);
        sphere.AddComponent<SphereColliderComponent>();
        sphere.AddComponent<PhysicsComponent>().SetDynamic();

        GameObject capsule = new GameObject(PrimitiveTypes.Capsule) { Name = "Capsule", };
        capsule.Transform.Position = new Vector3(-2, 10, 0);
        capsule.AddComponent<CapsuleColliderComponent>();
        capsule.AddComponent<PhysicsComponent>().SetDynamic();
    }

}
