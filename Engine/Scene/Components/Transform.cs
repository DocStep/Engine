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
    [Hide][JsonIgnore] public Action? de_Stop = null;

    [Hide][JsonIgnore] private Vector3 localPosition = Vector3.Zero;
    [Hide][JsonIgnore] private Quaternion localRotation = Quaternion.Identity;
    [Hide][JsonIgnore] private Vector3 localRotationEuler = Vector3.Zero;
    [Hide][JsonIgnore] private Vector3 localScale = Vector3.One;

    [Hide][JsonIgnore] private Vector3 rotationEuler = Vector3.Zero;

    [Hide][JsonIgnore]
    public Transform? Parent {
        get => parent;
        set {
            if (parent == value) return;

            Vector3 worldPosition = Position;
            Quaternion worldRotation = Rotation;

            parent?.Children.Remove(this);
            parent = value;

            if (parent is not null && !parent.Children.Contains(this))
                parent.Children.Add(this);

            SetPosition_Silent(worldPosition);
            SetRotation_Silent(worldRotation);

            de_PositionChanged?.Invoke(Position);
            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide][JsonIgnore]
    private Transform? parent = null;

    [Hide][JsonIgnore]
    public List<Transform> Children { get; } = new();


    /// ============================================================
    /// LOCAL
    /// ============================================================

    [JsonIgnore]
    public Vector3 LocalPosition {
        get => localPosition;
        set {
            if (localPosition == value) return;

            localPosition = value;

            de_PositionChanged?.Invoke(Position);
        }
    }

    [JsonIgnore][WrapRotation(0, 360)][ChangeStep(1f)]
    public Vector3 LocalRotation {
        get {
            localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
            return localRotationEuler;
        }
        set {
            value = WrapVector3(value, 0f, 360f);

            Vector3 oldEuler = localRotationEuler;

            float dx = ShortestAngle(oldEuler.X, value.X);
            float dy = ShortestAngle(oldEuler.Y, value.Y);
            float dz = ShortestAngle(oldEuler.Z, value.Z);

            if (MathF.Abs(dx) < 0.000001f &&
                MathF.Abs(dy) < 0.000001f &&
                MathF.Abs(dz) < 0.000001f)
                return;

            if (0.000001f <= MathF.Abs(dx))
                RotateLocalX_Silent(dx);

            if (0.000001f <= MathF.Abs(dy))
                RotateLocalY_Silent(dy);

            if (0.000001f <=MathF.Abs(dz))
                RotateLocalZ_Silent(dz);

            localRotationEuler = QuaternionToEuler(localRotation, value);
            rotationEuler = QuaternionToEuler(Rotation, rotationEuler);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide][JsonIgnore]
    public Quaternion LocalQuaternion {
        get => localRotation;
        set {
            SetLocalRotation_Silent(value);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [JsonIgnore]
    public Vector3 LocalScale {
        get => localScale;
        set {
            if (localScale == value) return;

            localScale = value;

            de_ScaleChanged?.Invoke(Scale);
        }
    }


    /// ============================================================
    /// WORLD
    /// ============================================================

    [Hide][JsonIgnore]
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

    [Hide][JsonIgnore]
    public Quaternion Rotation {
        get {
            if (Parent is null) return localRotation;

            return Quaternion.Normalize(localRotation*Parent.Rotation);
        }
        set {
            SetRotation_Silent(value);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide][JsonIgnore][WrapRotation(0, 360)][ChangeStep(1f)]
    public Vector3 RotationEuler {
        get {
            rotationEuler = QuaternionToEuler(Rotation, rotationEuler);
            return rotationEuler;
        }
        set {
            value = WrapVector3(value, 0f, 360f);

            SetRotation_Silent(EulerToQuaternion(value));

            rotationEuler = QuaternionToEuler(Rotation, value);

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
                parentScale.X != 0f ? value.X/parentScale.X : 0f,
                parentScale.Y != 0f ? value.Y/parentScale.Y : 0f,
                parentScale.Z != 0f ? value.Z/parentScale.Z : 0f
            );
        }
    }


    /// Matrices
    [Hide]
    [JsonIgnore]
    public Matrix4x4 LocalMatrix =>
        Matrix4x4.CreateScale(LocalScale)*
        Matrix4x4.CreateFromQuaternion(localRotation)*
        Matrix4x4.CreateTranslation(LocalPosition);
    [Hide]
    [JsonIgnore]
    public Matrix4x4 WorldMatrix =>
        Parent is null ? LocalMatrix : LocalMatrix*Parent.WorldMatrix;


    /// Sync
    public void SetPosition_Silent (Vector3 position) {
        if (Parent is null) {
            localPosition = position;
            return;
        }

        if (!Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 inverse)) {
            localPosition = position;
            return;
        }

        localPosition = Vector3.Transform(position, inverse);
    }
    public void SetLocalRotation_Silent (Quaternion rotation) {
        localRotation = NormalizeSafe(rotation);
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(Rotation, rotationEuler);
    }

    public void SetRotation_Silent (Quaternion rotation) {
        rotation = NormalizeSafe(rotation);
        Quaternion local = Parent is null ? rotation
            : Quaternion.Normalize(rotation*Quaternion.Inverse(Parent.Rotation));

        localRotation = local;
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(rotation, rotationEuler);
    }

    private static Quaternion NormalizeSafe (Quaternion rotation) {
        float lengthSquared =
            rotation.X*rotation.X +
            rotation.Y*rotation.Y +
            rotation.Z*rotation.Z +
            rotation.W*rotation.W;

        if (lengthSquared < 0.0000001f)
            return Quaternion.Identity;

        return Quaternion.Normalize(rotation);
    }


    /// Rotation

    public void RotateLocalX (float degrees) {
        RotateLocalX_Silent(degrees);
        de_RotationChanged?.Invoke(Rotation);
    }

    public void RotateLocalY (float degrees) {
        RotateLocalY_Silent(degrees);
        de_RotationChanged?.Invoke(Rotation);
    }

    public void RotateLocalZ (float degrees) {
        RotateLocalZ_Silent(degrees);
        de_RotationChanged?.Invoke(Rotation);
    }

    private void RotateLocalX_Silent (float degrees) {
        Quaternion delta = Quaternion.CreateFromAxisAngle(Vector3.UnitX, degrees*Mathf.Deg2Rad);
        localRotation = Quaternion.Normalize(localRotation*delta);
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(Rotation, rotationEuler);
    }

    private void RotateLocalY_Silent (float degrees) {
        Quaternion delta = Quaternion.CreateFromAxisAngle(Vector3.UnitY, degrees*Mathf.Deg2Rad);
        localRotation = Quaternion.Normalize(localRotation*delta);
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(Rotation, rotationEuler);
    }

    private void RotateLocalZ_Silent (float degrees) {
        Quaternion delta = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, degrees*Mathf.Deg2Rad);
        localRotation = Quaternion.Normalize(localRotation*delta);
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(Rotation, rotationEuler);
    }

    public void RotateWorldX (float degrees) {
        RotateWorld(Vector3.UnitX, degrees);
    }

    public void RotateWorldY (float degrees) {
        RotateWorld(Vector3.UnitY, degrees);
    }

    public void RotateWorldZ (float degrees) {
        RotateWorld(Vector3.UnitZ, degrees);
    }

    public void RotateWorld (Vector3 axis, float degrees) {
        float lengthSquared = axis.X*axis.X + axis.Y*axis.Y + axis.Z*axis.Z;
        if (lengthSquared < 0.0000001f) return;

        axis = Vector3.Normalize(axis);
        Quaternion delta = Quaternion.CreateFromAxisAngle(axis, degrees*Mathf.Deg2Rad);
        SetRotation_Silent(delta*Rotation);

        de_RotationChanged?.Invoke(Rotation);
    }


    /// Quaternion <-> Euler

    private static Quaternion EulerToQuaternion (Vector3 euler) {
        float x = euler.X*Mathf.Deg2Rad;
        float y = euler.Y*Mathf.Deg2Rad;
        float z = euler.Z*Mathf.Deg2Rad;

        Quaternion qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x);
        Quaternion qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
        Quaternion qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z);

        return Quaternion.Normalize(qz*qy*qx);
    }

    private static Vector3 QuaternionToEuler (Quaternion q, Vector3 previous) {
        q = NormalizeSafe(q);

        float m11 = 1f - 2f*(q.Y*q.Y + q.Z*q.Z);
        float m12 = 2f*(q.X*q.Y - q.Z*q.W);
        float m13 = 2f*(q.X*q.Z + q.Y*q.W);
        float m23 = 2f*(q.Y*q.Z - q.X*q.W);
        float m33 = 1f - 2f*(q.X*q.X + q.Y*q.Y);

        float x;
        float y = MathF.Asin(Math.Clamp(m13, -1, 1));
        float z;
        float cosY = MathF.Cos(y);

        if (0.000001f < MathF.Abs(cosY)) {
            x = MathF.Atan2(-m23, m33);
            z = MathF.Atan2(-m12, m11);
        } else {
            z = previous.Z*Mathf.Deg2Rad;
            if (0 < y) {
                x = MathF.Atan2(2f*(q.X*q.W + q.Y*q.Z), 1f - 2f*(q.X*q.X + q.Z*q.Z));
            } else {
                x = MathF.Atan2(-2f*(q.X*q.W + q.Y*q.Z), 1f - 2f*(q.X*q.X + q.Z*q.Z));
            }
        }
        Vector3 result = new Vector3(x*Mathf.Rad2Deg, y*Mathf.Rad2Deg, z*Mathf.Rad2Deg);

        return WrapVector3(result, 0, 360);
    }

    private static float ShortestAngle (float a, float b) {
        float d = (b - a)%360;

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
        if (range <= 0) return min;

        value = (value - min) % range;
        if (value < 0f) value += range;
        return value + min;
    }


    [Hide][JsonIgnore] public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    [Hide][JsonIgnore] public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
    [Hide][JsonIgnore] public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);


    //public void SetPosition (Vector3 position) {
    //    Position = position;
    //}

    //public void SetRotation (Quaternion rotation) {
    //    Rotation = rotation;
    //}

    //public void SetScale (Vector3 scale) {
    //    LocalScale = scale;
    //}

    public void Stop () {
        de_Stop?.Invoke();
    }


    public Matrix4x4 GetWorldMatrix () {
        return WorldMatrix;
    }

}