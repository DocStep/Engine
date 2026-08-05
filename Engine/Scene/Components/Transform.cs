using Newtonsoft.Json;

namespace Engine;

public class Transform : Component {

#pragma warning disable CS0108
    [Hide] public bool Enabled { get; set; } = true;
#pragma warning restore CS0108

    [JsonIgnore] public override string Name => nameof(Transform);

    public Vector3 Position = Vector3.Zero;
    [Hide][JsonIgnore] public Quaternion rotation = Quaternion.Identity;
    [DrawName("Rotation")][JsonIgnore] public Vector3 rotationEuler;
    [Hide][JsonIgnore][WrapRotation(0, 360)][ChangeStep(1f)] public Vector3 RotationEuler {
        get => rotationEuler;
        set {
            rotationEuler = Mathf.WrapVector3(value, 0, 360);
            rotation = Mathf.QuaternionFromEuler(rotationEuler);
        }
    }
    public Vector3 Scale = Vector3.One;

    [JsonIgnore] public Vector3 Right => Vector3.Transform(Vector3.UnitX, rotation);
    [JsonIgnore] public Vector3 Up => Vector3.Transform(Vector3.UnitY, rotation);
    [JsonIgnore] public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, rotation);

    public static float DegreesToRadians (float angle) => angle*Mathf.Deg2Rad;


    public void SetPosition (Vector3 position) {
        Position = position;

        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null)
            physicsComponent.Rigidbody.Position = Position;
    }

    public void SetRotation (Vector3 euler) {
        rotation = Mathf.JQuaternionFromEuler(Mathf.WrapVector3(euler, 0, 360));

        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null)
            physicsComponent.Rigidbody.Orientation = rotation;
    }

    public void SetRotation (Quaternion rotation) {
        rotation = Quaternion.Normalize(rotation);

        PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null)
            physicsComponent.Rigidbody.Orientation = rotation;
    }

    public void SetScale (Vector3 scale) {
        Scale = scale;
    }

    public void Stop () {
        owner.GetComponent<PhysicsComponent>()?.Stop();
    }


    public static Vector3 EulerFromQuaternion (Quaternion q) {
        Matrix4x4 m = Matrix4x4.CreateFromQuaternion(q);

        float pitch = MathF.Asin(-m.M23);

        float yaw;
        float roll;

        if (MathF.Abs(m.M23) < 0.999999f) {
            yaw = MathF.Atan2(m.M13, m.M33);
            roll = MathF.Atan2(m.M21, m.M22);
        } else {
            yaw = MathF.Atan2(-m.M31, m.M11);
            roll = 0;
        }

        return new Vector3(
            pitch*Mathf.Rad2Deg,
            yaw*Mathf.Rad2Deg,
            roll*Mathf.Rad2Deg);
    }
    public static Quaternion QuaternionFromEuler (Vector3 euler) {
        return Quaternion.CreateFromYawPitchRoll(
            euler.Y*Mathf.Deg2Rad,
            euler.X*Mathf.Deg2Rad,
            euler.Z*Mathf.Deg2Rad);
    }

    public Matrix4x4 GetWorldMatrix () {
        Matrix4x4 scaleMat = Matrix4x4.CreateScale(Scale);
        Matrix4x4 rotMat = Matrix4x4.CreateFromQuaternion(rotation);
        Matrix4x4 transMat = Matrix4x4.CreateTranslation(Position);
        return scaleMat*rotMat*transMat;
    }

}