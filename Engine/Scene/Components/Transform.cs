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
    private Transform? parent = null;

    [Hide][JsonIgnore]
    public List<Transform> Children { get; } = new List<Transform>();


    [Hide][JsonIgnore]
    public Transform? Parent {
        get => parent;
        set {
            if (parent == value || value == this) return;

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

    /// <summary> Use <see cref="RotateLocalEuler"/> for continuous rotation </summary>
    [JsonIgnore][WrapRotation(0, 360)][ChangeStep(1f)]
    public Vector3 LocalEuler {
        get => localRotationEuler;
        set {
            value = WrapVector3(value, 0, 360);

            localRotationEuler = value;
            localRotation = EulerToQuaternion(value);
            rotationEuler = QuaternionToEuler(Rotation, rotationEuler);

            de_RotationChanged?.Invoke(Rotation);
        }
    }
    public void RotateLocalEuler (Vector3 degrees) {
        Quaternion delta =
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, degrees.X*Mathf.Deg2Rad)*
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, degrees.Y*Mathf.Deg2Rad)*
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, degrees.Z*Mathf.Deg2Rad);

        RotateLocal(delta);
    }

    public void RotateLocal (Quaternion delta) {
        localRotation = Quaternion.Normalize(localRotation*delta);
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(Rotation, rotationEuler);

        de_RotationChanged?.Invoke(Rotation);
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

            return Quaternion.Normalize(Parent.Rotation*localRotation);
        }
        set {
            SetRotation_Silent(value);

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide][JsonIgnore][WrapRotation(0, 360)][ChangeStep(1f)]
    public Vector3 RotationEuler {
        get => rotationEuler;
        set {
            value = WrapVector3(value, 0f, 360f);

            SetRotation_Silent(EulerToQuaternion(value));

            rotationEuler = value;

            de_RotationChanged?.Invoke(Rotation);
        }
    }

    [Hide][JsonIgnore]
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
    [Hide][JsonIgnore]
    public Matrix4x4 LocalMatrix =>
        Matrix4x4.CreateScale(LocalScale)*
        Matrix4x4.CreateFromQuaternion(localRotation)*
        Matrix4x4.CreateTranslation(LocalPosition);
    [Hide][JsonIgnore]
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
            : Quaternion.Normalize(Quaternion.Inverse(Parent.Rotation)*rotation);

        localRotation = local;
        localRotationEuler = QuaternionToEuler(localRotation, localRotationEuler);
        rotationEuler = QuaternionToEuler(rotation, rotationEuler);
    }

    public void CopyFrom (Transform source) {
        localPosition = source.localPosition;
        localRotation = source.localRotation;
        localRotationEuler = source.localRotationEuler;
        localScale = source.localScale;
        rotationEuler = source.rotationEuler;

        de_PositionChanged = source.de_PositionChanged;
        de_RotationChanged = source.de_RotationChanged;
        de_ScaleChanged = source.de_ScaleChanged;
        de_Stop = source.de_Stop;

        parent = source.parent;
        if (parent is not null) {
            int childIndex = parent.Children.IndexOf(source);
            if (0 <= childIndex) parent.Children[childIndex] = this;
            else if (!parent.Children.Contains(this)) parent.Children.Add(this);
        }

        Children.Clear();
        Children.AddRange(source.Children);
        foreach (Transform child in Children)
            child.parent = this;

        source.Children.Clear();
        source.parent = null;
        source.de_PositionChanged = null;
        source.de_RotationChanged = null;
        source.de_ScaleChanged = null;
        source.de_Stop = null;
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


    /// ============================================================
    /// LOOK ROTATION (twist-free direction-based rotation)
    /// ============================================================

    public void SetForward (Vector3 forward, Vector3? worldUp = null) {
        Quaternion rotation = LookRotation(forward, worldUp ?? Vector3.UnitY);
        Rotation = rotation;
    }

    public void SetLocalForward (Vector3 forward, Vector3? worldUp = null) {
        Quaternion rotation = LookRotation(forward, worldUp ?? Vector3.UnitY);
        LocalQuaternion = rotation;
    }

    public static Quaternion LookRotation (Vector3 forward, Vector3 worldUp) {
        float lengthSquared = forward.X*forward.X + forward.Y*forward.Y + forward.Z*forward.Z;
        if (lengthSquared < 0.0000001f) return Quaternion.Identity;

        forward = Vector3.Normalize(forward);

        /// guard against forward parallel to worldUp
        if (0.9999f < MathF.Abs(Vector3.Dot(forward, worldUp)))
            worldUp = MathF.Abs(forward.Y) < 0.9999f ? Vector3.UnitY : Vector3.UnitZ;

        Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        Vector3 up = Vector3.Cross(forward, right);

        Matrix4x4 basis = new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            0f, 0f, 0f, 1f
        );

        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(basis));
    }


    /// ============================================================
    /// SWING-TWIST DECOMPOSITION (debug / analysis)
    /// ============================================================

    public static void SwingTwist (Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist) {
        twistAxis = Vector3.Normalize(twistAxis);

        Vector3 rotationAxis = new Vector3(rotation.X, rotation.Y, rotation.Z);
        float dot = Vector3.Dot(rotationAxis, twistAxis);

        Vector3 projection = twistAxis*dot;
        twist = Quaternion.Normalize(new Quaternion(projection.X, projection.Y, projection.Z, rotation.W));

        if (twist.X*twist.X + twist.Y*twist.Y + twist.Z*twist.Z + twist.W*twist.W < 0.0000001f)
            twist = Quaternion.Identity;

        swing = rotation*Quaternion.Inverse(twist);
    }

    public static float TwistAngleDegrees (Quaternion rotation, Vector3 twistAxis) {
        SwingTwist(rotation, twistAxis, out _, out Quaternion twist);
        float angle = 2f*MathF.Acos(Math.Clamp(twist.W, -1f, 1f));
        if (180f < angle*Mathf.Rad2Deg) angle -= 2f*MathF.PI;
        return angle*Mathf.Rad2Deg;
    }


    /// Quaternion <-> Euler
    /// Composition order: Ry(yaw) * Rx(pitch) * Rz(roll). Y is applied outermost
    /// so it never twists the object about its own forward axis; X only pitches
    /// within the plane already set by Y; Z is a pure, isolated roll.

    private static Quaternion EulerToQuaternion (Vector3 euler) {
        float pitch = euler.X*Mathf.Deg2Rad;
        float yaw = euler.Y*Mathf.Deg2Rad;
        float roll = euler.Z*Mathf.Deg2Rad;

        Quaternion qYaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        Quaternion qPitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        Quaternion qRoll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll);

        return Quaternion.Normalize(qYaw*qPitch*qRoll);
    }

    private static Vector3 QuaternionToEuler (Quaternion q, Vector3 previous) {
        q = NormalizeSafe(q);

        float sinYaw = 2f*(q.W*q.Y - q.Z*q.X);
        sinYaw = Math.Clamp(sinYaw, -1f, 1f);

        float x;
        float y;
        float z;

        if (MathF.Abs(sinYaw) < 0.9999f) {
            y = MathF.Asin(sinYaw);
            x = MathF.Atan2(2f*(q.W*q.X + q.Y*q.Z), 1f - 2f*(q.X*q.X + q.Y*q.Y));
            z = MathF.Atan2(2f*(q.W*q.Z + q.X*q.Y), 1f - 2f*(q.Y*q.Y + q.Z*q.Z));
        } else {
            y = MathF.Asin(sinYaw);
            z = previous.Z*Mathf.Deg2Rad;

            float m00 = 1f - 2f*(q.Y*q.Y + q.Z*q.Z);
            float m10 = 2f*(q.X*q.Y + q.Z*q.W);

            x = sinYaw > 0f
                ? MathF.Atan2(m10, m00) - z
                : MathF.Atan2(-m10, m00) + z;
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
