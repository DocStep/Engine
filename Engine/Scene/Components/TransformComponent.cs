using System;
using System.Numerics;

namespace Engine;


public class TransformComponent : Component {

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    public Vector3 Right => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, Matrix4x4.RotationEuler(Rotation)));
    public Vector3 Up => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, Matrix4x4.RotationEuler(Rotation)));
    public Vector3 Forward => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, Matrix4x4.RotationEuler(Rotation)));


    public Matrix4x4 GetWorldMatrix () {
        var scaleMat = Matrix4x4.CreateScale(Scale);
        var rotMat = Matrix4x4.CreateFromYawPitchRoll(DegreesToRadians(Rotation.Y),
            DegreesToRadians(Rotation.X), DegreesToRadians(Rotation.Z));
        var transMat = Matrix4x4.CreateTranslation(Position);
        return scaleMat*rotMat*transMat;
    }

    public const float eulerRad = MathF.PI/180f;
    public static float DegreesToRadians (float angle) => angle*eulerRad;

}
