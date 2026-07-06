using Newtonsoft.Json;

namespace Engine;


public class TransformComponent : Component {

    [JsonIgnore] public readonly static string typeName = typeof(TransformComponent).Name;

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    [JsonIgnore] public Vector3 Right => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, Matrix4x4.RotationEuler(Rotation)));
    [JsonIgnore] public Vector3 Up => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, Matrix4x4.RotationEuler(Rotation)));
    [JsonIgnore] public Vector3 Forward => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, Matrix4x4.RotationEuler(Rotation)));

    public static float DegreesToRadians (float angle) => angle*lib.Deg2Rad;


    public void SetPosition (Vector3 position) {
        Position = position;

        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null) {
            physicsComponent.Rigidbody.Position = Position;
            //PhysicsManager.Instance.World.Stabilize(0.01f, 100, 100);
        }
    }
    public void SetRotation (Vector3 rotation) {
        Rotation = rotation;

        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null) {
            physicsComponent.Rigidbody.Orientation = Utils.QuaternionFromEuler(rotation);
            //PhysicsManager.Instance.World.Stabilize(0.01f, 100, 100);
        }
    }
    public void SetScale (Vector3 scale) {
        Scale = scale;
    }
    public void Stop () {
        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        physicsComponent?.Stop();
    }


    public Matrix4x4 GetWorldMatrix () {
        var scaleMat = Matrix4x4.CreateScale(Scale);
        var rotMat = Matrix4x4.CreateFromYawPitchRoll(lib.Deg2Rad*Rotation.Y, 
            lib.Deg2Rad*Rotation.X, lib.Deg2Rad*Rotation.Z);
        var transMat = Matrix4x4.CreateTranslation(Position);
        return scaleMat*rotMat*transMat;
    }

}
