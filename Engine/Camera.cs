using Silk.NET.Maths;
using Engine.Graphics;
using Engine.Input;

namespace Engine;


internal class Camera {
    internal Camera () {
        Instance = this;

        Engine.Window.Update += Update;

        UpdateCamera(0);
    }

    public static Camera Instance = null!;

    /// Values
    protected const float _sensetivity = 0.15f;
    internal const float _sensetivityMultiplier = 0.01f;

    internal Vector3D<float> cameraPos = Vector3D<float>.Zero;
    internal Matrix4X4<float> cameraRot = Matrix4X4<float>.Identity;
    internal Vector2D<float> mousePos = Vector2D<float>.Zero;

    protected float yaw;
    protected float pitch;


    protected virtual void Update (double deltaTime) {
        if (Inputs.Actions[Inputs.NavBack].pressedDown) Engine.Window.Close();

        float dt = (float)deltaTime;
        float dx = Inputs.MouseDelta.X;
        float dy = Inputs.MouseDelta.Y;

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        Vector3D<float> forward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);
        Vector3D<float> right = Vector3D.Transform(Vector3D<float>.UnitX, cameraRot);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, cameraRot);
        Vector3D<float> cameraPosDelta = Vector3D<float>.Zero;

        float flipSign = 1f;
        yaw += -_sensetivityMultiplier*_sensetivity*dx*flipSign;
        pitch += -_sensetivityMultiplier*_sensetivity*dy;
        pitch = Utils.WrapAngle(pitch);

        UpdateCamera(deltaTime);
    }


    protected virtual void UpdateCamera (double deltaTime) {
        Matrix4X4<float> rotation;
        Vector3D<float> forward;

        Vector3D<float> worldUp = MathF.Cos(pitch) < 0 ? -Vector3D<float>.UnitY : Vector3D<float>.UnitY;

        /// Center Rotation
        rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);

        Inputs.MouseHide();

        Renderer.Instance.View = Matrix4X4.CreateLookAt(cameraPos, cameraPos + forward, worldUp);
        cameraRot = rotation;
    }


}
