using Newtonsoft.Json;

namespace Engine;

public class Transform : Component {

#pragma warning disable CS0108
    [Hide] public bool Enabled { get; set; } = true;
#pragma warning restore CS0108

    public override string Name => nameof(Transform);

    [Hide][JsonIgnore] public Action<Vector3>? de_PositionChanged = null;
    [Hide][JsonIgnore] public Action<Quaternion>? de_RotationChanged = null;
    [Hide][JsonIgnore] public Action<Vector3>? de_ScaleChanged = null;

    [Hide][JsonIgnore] private Vector3 localPosition = Vector3.Zero;
    [Hide][JsonIgnore] private Quaternion localRotation = Quaternion.Identity;
    [Hide][JsonIgnore] private Vector3 localRotationEuler = Vector3.Zero;
    [Hide][JsonIgnore] private Vector3 localScale = Vector3.One;

    [Hide][JsonIgnore] public Transform? Parent {
        get => parent;
        set {
            if (parent == value) return;

            Vector3 worldPosition = Position;
            Quaternion worldRotation = Rotation;

            parent?.Children.Remove(this);
            parent = value;

            if (parent is not null && !parent.Children.Contains(this))
                parent.Children.Add(this);

            Position = worldPosition;
            Rotation = worldRotation;
        }
    }

    [Hide]
    [JsonIgnore]
    private Transform? parent = null;

    [Hide]
    [JsonIgnore]
    public List<Transform> Children { get; } = new();


    /// ============================================================
    /// LOCAL
    /// ============================================================

    [JsonIgnore]
    public Vector3 LocalPosition {
        get => localPosition;
        set {
            localPosition = value;

            de_PositionChanged?.Invoke(Position);
        }
    }

    [JsonIgnore]
    [WrapRotation(0, 360)]
    [ChangeStep(1f)]
    public Vector3 LocalRotation {
        get => localRotationEuler;
        set {
            localRotationEuler = WrapVector3(value, 0f, 360f);
            localRotation = EulerToQuaternion(localRotationEuler);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide]
    [JsonIgnore]
    public Quaternion LocalQuaternion {
        get => localRotation;
        set {
            localRotation = Quaternion.Normalize(value);
            localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [JsonIgnore]
    public Vector3 LocalScale {
        get => localScale;
        set => localScale = value;
    }


    /// ============================================================
    /// WORLD
    /// ============================================================

    [Hide]
    [JsonIgnore]
    public Vector3 Position {
        get {
            if (Parent is null) return LocalPosition;

            return Vector3.Transform(LocalPosition, Parent.WorldMatrix);
        }
        set {
            SetPosition_Silent(value);

            de_PositionChanged?.Invoke(Position);
        }
    }

    [Hide]
    [JsonIgnore]
    public Quaternion Rotation {
        get {
            if (Parent is null) return localRotation;

            return Quaternion.Normalize(Parent.Rotation*localRotation);
        }
        set {
            SetRotation_Silent(value);
            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide]
    [JsonIgnore]
    public Vector3 Scale {
        get {
            if (Parent is null) return LocalScale;

            Vector3 parentScale = Parent.Scale;
            return new Vector3(
                LocalScale.X*parentScale.X, 
                LocalScale.Y*parentScale.Y, 
                LocalScale.Z*parentScale.Z);
        }
        set {
            if (Parent is null) {
                LocalScale = value;
                return;
            }

            Vector3 parentScale = Parent.Scale;
            LocalScale = new Vector3(
                parentScale.X != 0f ? value.X / parentScale.X : 0f,
                parentScale.Y != 0f ? value.Y / parentScale.Y : 0f,
                parentScale.Z != 0f ? value.Z / parentScale.Z : 0f
            );
        }
    }


    /// ============================================================
    /// MATRICES
    /// ============================================================

    [Hide]
    [JsonIgnore]
    public Matrix4x4 LocalMatrix =>
        Matrix4x4.CreateScale(LocalScale)*
        Matrix4x4.CreateFromQuaternion(localRotation)*
        Matrix4x4.CreateTranslation(LocalPosition);

    [Hide]
    [JsonIgnore]
    public Matrix4x4 WorldMatrix =>
        Parent is null ? LocalMatrix : LocalMatrix * Parent.WorldMatrix;


    /// ============================================================
    /// SYNC
    /// ============================================================

    public void SetPosition_Silent (Vector3 position) {
        if (Parent is null) {
            LocalPosition = position;
            return;
        }

        Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 inverse);
        LocalPosition = Vector3.Transform(position, inverse);
    }
    public void SetRotation_Silent (Quaternion rotation) {
        Quaternion local = Parent is null ? Quaternion.Normalize(rotation)
                : Quaternion.Normalize(rotation*Quaternion.Inverse(Parent.Rotation));
        localRotation = local;
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
    }



    /// ============================================================
    /// QUATERNION <-> EULER
    /// ============================================================
    
    private static Quaternion EulerToQuaternion (Vector3 euler) {
        float x = 0.5f*euler.X*Mathf.Deg2Rad;
        float y = 0.5f*euler.Y*Mathf.Deg2Rad;
        float z = 0.5f*euler.Z*Mathf.Deg2Rad;

        float sx = MathF.Sin(x);
        float cx = MathF.Cos(x);

        float sy = MathF.Sin(y);
        float cy = MathF.Cos(y);

        float sz = MathF.Sin(z);
        float cz = MathF.Cos(z);

        return Quaternion.Normalize(
            new Quaternion(
                sx*cy*cz - cx*sy*sz,
                cx*sy*cz + sx*cy*sz,
                cx*cy*sz - sx*sy*cz,
                cx*cy*cz + sx*sy*sz
            )
        );
    }
    private static Vector3 QuaternionToEuler (Quaternion q, Vector3 previous) {
        q = Quaternion.Normalize(q);

        float sinX = 2f*(q.W*q.X + q.Y*q.Z);
        float cosX = 1f - 2f*(q.X*q.X + q.Y*q.Y);

        float sinY = 2f*(q.W*q.Y - q.Z*q.X);
        sinY = Math.Clamp(sinY, -1, 1);

        float sinZ = 2f*(q.W*q.Z + q.X*q.Y);
        float cosZ = 1f - 2f*(q.Y*q.Y + q.Z*q.Z);

        float x = MathF.Atan2(sinX, cosX);
        float y = MathF.Asin(sinY);
        float z = MathF.Atan2(sinZ, cosZ);

        Vector3 a = new Vector3(x*Mathf.Rad2Deg, y*Mathf.Rad2Deg, z*Mathf.Rad2Deg);
        Vector3 b = new Vector3(a.X + 180, 180 - a.Y, a.Z + 180); /// The second valid Euler solution.

        a = WrapVector3(a, 0, 360);
        b = WrapVector3(b, 0, 360);

        return DistanceSquared(a, previous) <= DistanceSquared(b, previous) ? a : b;
    }
    private static float DistanceSquared (Vector3 a, Vector3 b) {
        float x = ShortestAngle(a.X, b.X);
        float y = ShortestAngle(a.Y, b.Y);
        float z = ShortestAngle(a.Z, b.Z);
        return x*x + y*y + z*z;
    }
    private static float ShortestAngle (float a, float b) {
        float d = (a - b) % 360;
        if (180 < d) d -= 360;
        if (d < -180) d += 360;
        return d;
    }


    private static Vector3 WrapVector3 (Vector3 value, float min, float max) {
        return new Vector3(
            Wrap(value.X, min, max),
            Wrap(value.Y, min, max),
            Wrap(value.Z, min, max)
        );
    }

    private static float Wrap (float value, float min, float max) {
        float range = max - min;
        value = (value - min) % range;
        if (value < 0f) value += range;
        return value + min;
    }


    [Hide][JsonIgnore] public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    [Hide][JsonIgnore] public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
    [Hide][JsonIgnore] public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);


    public void SetPosition (Vector3 position) {
        Position = position;
        gameObject.GetComponent<PhysicsComponent>()?.SetPosition(position);
    }
    public void SetRotation (Quaternion rotation) {
        SetRotation_Silent(rotation);
        gameObject.GetComponent<PhysicsComponent>()?.SetRotation(rotation);
    }
    public void SetScale (Vector3 scale) {
        LocalScale = scale;
        gameObject.GetComponent<PhysicsComponent>()?.SetScale(scale);
    }

    public void Stop () {
        gameObject.GetComponent<PhysicsComponent>()?.Stop();
    }



    public Matrix4x4 GetWorldMatrix () {
        return WorldMatrix;
    }

}