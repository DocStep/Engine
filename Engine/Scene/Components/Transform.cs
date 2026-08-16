using Newtonsoft.Json;

namespace Engine;

public class Transform : Component {

#pragma warning disable CS0108
    [Hide] public bool Enabled { get; set; } = true;
#pragma warning restore CS0108

    public override string Name => nameof(Transform);

    [Hide][JsonIgnore] private Transform? parent = null;
    [Hide][JsonIgnore]
    public Transform? Parent {
        get => parent;
        set {
            if (parent == value) return;

            parent?.Children.Remove(this);
            if (value is not null && !value.Children.Contains(this)) 
                value.Children.Add(this);
            parent = value;
        }
    }
    [Hide] public Guid? parentGuid = null;
    [Hide][JsonIgnore] public List<Transform> Children = new List<Transform>();
    [Hide] public List<Guid> childrenGuids = new List<Guid>();

    public Vector3 Position = Vector3.Zero;
    [Hide][JsonIgnore] public Quaternion rotation = Quaternion.Identity;
    [Hide][JsonIgnore] public Quaternion Rotation {
        get => rotation;
        set {
            rotation = value;
            rotationEuler = Mathf.QuaternionToEuler(rotation);
        }
    }
    [Hide] public Vector3 rotationEuler;
    [JsonIgnore][DrawName("Rotation")][WrapRotation(0, 360)][ChangeStep(1f)]
    public Vector3 RotationEuler {
        get => rotationEuler;
        set {
            rotationEuler = Mathf.WrapVector3(value, 0, 360);
            rotation = Mathf.EulerToQuaternion(rotationEuler);
        }
    }
    public Vector3 Scale = Vector3.One;

    [Hide][JsonIgnore] public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    [Hide][JsonIgnore] public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
    [Hide][JsonIgnore] public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);


    public void SetPosition (Vector3 position) {
        Position = position;
        gameObject.GetComponent<PhysicsComponent>()?.SetPosition(position);
    }
    public void SetRotation (Quaternion rotation) {
        Rotation = rotation;
        gameObject.GetComponent<PhysicsComponent>()?.SetRotation(rotation);
    }
    public void SetScale (Vector3 scale) {
        Scale = scale;
        gameObject.GetComponent<PhysicsComponent>()?.SetScale(scale);
    }

    public void Stop () {
        gameObject.GetComponent<PhysicsComponent>()?.Stop();
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
        Matrix4x4 rotMat = Matrix4x4.CreateFromQuaternion(Rotation);
        Matrix4x4 transMat = Matrix4x4.CreateTranslation(Position);
        return scaleMat*rotMat*transMat;
    }

}