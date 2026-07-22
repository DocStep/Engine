namespace Engine;


public class ScenePhysics : Scene {
    public ScenePhysics () {

    }

    public override void Load () {
        GameObject ground = new GameObject() { Name = "Plane", };
        ground.Transform.Position = new Vector3(0, 0, 0);
        ground.Transform.Rotation = new Vector3(0, 30, 0);
        ground.Transform.Scale = new Vector3(10, 1f, 10);
        ground.AddComponent<Graphics.MeshComponent>().mesh = AssetsEngine._mesh_Cube;
        ground.AddComponent<BoxColliderComponent>();
        ground.AddComponent<PhysicsComponent>().Rigidbody.MotionType = Jitter2.Dynamics.MotionType.Static;
        /*
                GameObject cube = new GameObject() { Name = "Cube", };
                cube.Transform.Position = new Vector3(0, 1, 0);
                cube.Transform.Rotation = new Vector3(0, 1, 0);
                cube.AddComponent<MeshComponent>().mesh = Renderer.Instance._mesh_Cube;
                cube.AddComponent<BoxColliderComponent>();
                cube.AddComponent<PhysicsComponent>().Rigidbody.MotionType = Jitter2.Dynamics.MotionType.Dynamic;
        */

        GameObject cube = new GameObject() { Name = "Cube", };
        cube.Transform.Position = new Vector3(0, 10, 0);
        //cube.Transform.Rotation = new Vector3(30, 0, 0);
        cube.AddComponent<Graphics.MeshComponent>().mesh = AssetsEngine._mesh_Cube;
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>().Rigidbody.MotionType = Jitter2.Dynamics.MotionType.Dynamic;

        cube = new GameObject() { Name = "cube", };
        cube.Transform.Position = new Vector3(0, 15, 0);
        cube.Transform.Rotation = new Vector3(30, 0, 0);
        cube.AddComponent<Graphics.MeshComponent>().mesh = AssetsEngine._mesh_Cube;
        cube.AddComponent<BoxColliderComponent>();
        cube.AddComponent<PhysicsComponent>().Rigidbody.MotionType = Jitter2.Dynamics.MotionType.Dynamic;

    }

}
