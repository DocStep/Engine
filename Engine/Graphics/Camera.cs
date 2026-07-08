//using Engine.Graphics;

namespace Engine.Graphics;


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

    public Vector3 cameraPos = Vector3.Zero;
    public Matrix4x4 cameraRot = Matrix4x4.Identity;
    public Vector2 mousePos = Vector2.Zero;

    public Vector3 forward => Vector3.Transform(Vector3.UnitZ, cameraRot);

    protected float yaw;
    protected float pitch;


    protected virtual void Update (double deltaTime) {
        
    }


    protected virtual void UpdateCamera (double deltaTime) {
        
    }


    public Ray RaycastMouse () {
        Ray ray = Raycast.ScreenPointToRay(mousePos.X, mousePos.Y, Engine.Window.Size.X, Engine.Window.Size.Y,
                Graphics.Renderer.Instance.m4x4_View, Graphics.Renderer.Instance.m4x4Projection);
        return ray;
    }

}
