using System.Numerics;
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

    internal Vector3 cameraPos = Vector3.Zero;
    internal Matrix4x4 cameraRot = Matrix4x4.Identity;
    internal Vector2 mousePos = Vector2.Zero;

    protected float yaw;
    protected float pitch;


    protected virtual void Update (double deltaTime) {
        if (Inputs.Actions[Inputs.NavBack].pressedDown) Engine.Window.Close();

        float dt = (float)deltaTime;
        float dx = Inputs.MouseDelta.X;
        float dy = Inputs.MouseDelta.Y;

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, cameraRot);
        Vector3 right = Vector3.Transform(Vector3.UnitX, cameraRot);
        Vector3 up = Vector3.Transform(Vector3.UnitY, cameraRot);
        Vector3 cameraPosDelta = Vector3.Zero;

        float flipSign = 1f;
        yaw += -_sensetivityMultiplier*_sensetivity*dx*flipSign;
        pitch += -_sensetivityMultiplier*_sensetivity*dy;
        pitch = Utils.WrapAngle(pitch);

        UpdateCamera(deltaTime);
    }


    protected virtual void UpdateCamera (double deltaTime) {
        Matrix4x4 rotation;
        Vector3 forward;

        Vector3 worldUp = MathF.Cos(pitch) < 0 ? -Vector3.UnitY : Vector3.UnitY;

        /// Center Rotation
        rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        forward  = Vector3.Transform(-Vector3.UnitZ, rotation);

        Inputs.MouseHide();
        
        Renderer.Instance.View = Matrix4x4.CreateLookAt(cameraPos, cameraPos + forward, worldUp);
        cameraRot = rotation;
    }


}
