using Engine.Graphics;
using Engine.Graphics;
using Engine.Graphics;

//using Engine.Graphics;

namespace Engine.Graphics;


public class Camera {
    public Camera () {
        Instance = this;

        Engine.Window.Update += Update;

        UpdateCamera(0);
    }

    public static Camera Instance = null!;

    /// Values
    protected const float _sensetivity = 0.10f;
    internal const float _sensetivityMultiplier = 0.01f;

    public Vector3 cameraPos = Vector3.Zero;
    public Matrix4x4 cameraRot = Matrix4x4.Identity;
    public Vector2 mousePos = Vector2.Zero;

    public Vector3 forward => Vector3.Transform(Vector3.UnitZ, cameraRot);

    protected float yaw;
    protected float pitch;

    public static float FOV = 60;
    public static float planeNear = 0.1f;
    public static float planeFar = 1000f;


    protected virtual void Update (double deltaTime) {
        
    }


    protected virtual void UpdateCamera (double deltaTime) {
        
    }


    public Ray? RaycastMouse () {
        Vector2? scenePos = EditorUI.Instance.GetSceneMousePos(mousePos);
        if (scenePos is null) return null;

        Vector2 sceneSize = EditorUI.Instance.SceneAvail;

        Ray ray = Raycast.ScreenPointToRay(scenePos.Value.X, scenePos.Value.Y, (int)sceneSize.X, (int)sceneSize.Y,
            Renderer.Instance.m4x4_View, Renderer.Instance.m4x4Projection);
        return ray;
    }

}
