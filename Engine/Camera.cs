using Silk.NET.Input;
using Silk.NET.Maths;
using Engine.Graphics;

namespace Engine;


internal class Camera {
    internal Camera () {
        Instance = this;

        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (mouse is not null) {
            previousMouseX = mouse.Position.X;
            previousMouseY = mouse.Position.Y;
        }

        Engine.Window.Update += Update;

        UpdateCamera(0);
    }

    public static Camera Instance = null!;

    /// Values
    protected const float _sensetivity = 0.15f;
    internal const float _sensetivityMultiplier = 0.01f;

    internal Vector3D<float> cameraPos = Vector3D<float>.Zero;
    internal Matrix4X4<float> cameraRot = Matrix4X4<float>.Identity;

    protected float yaw;
    protected float pitch;

    protected float previousMouseX;
    protected float previousMouseY;


    protected virtual void Update (double deltaTime) {
        var keyboard = Engine.Instance.Input.Keyboards.FirstOrDefault();
        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        if (keyboard.IsKeyPressed(Key.Escape)) Engine.Window.Close();

        float dt = (float)deltaTime;
        float mouseX = mouse.Position.X;
        float mouseY = mouse.Position.Y;

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        //float posDeltaL = MathF.Max(0, (cameraOrbitCenterPos - cameraPos).Length);
        Vector3D<float> forward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);
        Vector3D<float> right = Vector3D.Transform(Vector3D<float>.UnitX, cameraRot);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, cameraRot);
        Vector3D<float> cameraPosDelta = Vector3D<float>.Zero;

        float dx = mouseX - previousMouseX;
        float dy = mouseY - previousMouseY;

        float flipSign = 1f;
        yaw += -_sensetivityMultiplier*_sensetivity*dx*flipSign;
        pitch += -_sensetivityMultiplier*_sensetivity*dy;
        pitch = Utils.WrapAngle(pitch);

        UpdateCamera(deltaTime);

        previousMouseX = mouseX;
        previousMouseY = mouseY;
    }


    protected virtual void UpdateCamera (double deltaTime) {
        var keyboard = Engine.Instance.Input.Keyboards.FirstOrDefault();
        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        bool lmb = mouse.IsButtonPressed(MouseButton.Left);
        bool rmb = mouse.IsButtonPressed(MouseButton.Right);
        bool alt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

        Matrix4X4<float> rotation;
        Vector3D<float> forward;

        Vector3D<float> worldUp = MathF.Cos(pitch) < 0 ? -Vector3D<float>.UnitY : Vector3D<float>.UnitY;

        /// Center Rotation
        rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);

        mouse.Cursor.CursorMode = CursorMode.Raw;

        Renderer.Instance.View = Matrix4X4.CreateLookAt(cameraPos, cameraPos + forward, worldUp);
        cameraRot = rotation;
    }


}
