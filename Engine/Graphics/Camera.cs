using Engine.Graphics;

//using Engine.Graphics;

namespace Engine.Graphics;


public class Camera : Component, IComponentUpdate {
    public Camera () {
        Cameras.Insert(0, this);
        priority = 0;
        Log.log(Name, priority);
    }

    public override string Name { get; } = nameof(Camera);

    public static List<Camera> Cameras { get; private set; } = new List<Camera>();
    public static Camera? Current {
        get {
            return 0 < Cameras.Count ? Cameras[0] : null;
        }
    }


    /// Values
    protected const float _sensetivity = 0.10f;
    public const float _sensetivityMultiplier = 0.01f;

    public Vector3 cameraPos = Vector3.Zero;
    public Matrix4x4 cameraRot = Matrix4x4.Identity;
    public Vector2 mousePos_Window = Vector2.Zero;

    public Vector3 forward => Vector3.Transform(Vector3.UnitZ, cameraRot);

    protected float yaw;
    protected float pitch;

    public float FOV = 60;
    public float planeNear = 0.1f;
    public float planeFar = 1000f;
    private float _priority = 0f;
    public float priority {
        get => _priority;
        set {
            if (priority == value) return;

            priority = value;
            int posOld = Cameras.IndexOf(this);
            int posNew = 0;
            int count = Cameras.Count-1;
            for (int i = count-1; 0 <= i; i--) {
                if (priority < Cameras[i].priority) {
                    posNew = i;
                    break;
                }
            }
            if (posOld != posNew) {
                Cameras.RemoveAt(posOld);
                Cameras.Insert(posNew, this);
            }
        }
    }


    public virtual void Update () {
        Vector3 pos = owner.Transform.Position;
        Vector3 worldUp = MathF.Cos(pitch) < 0 ? -Vector3.UnitY : Vector3.UnitY;
        Renderer.Instance.m4x4_View = Matrix4x4.CreateLookAtLeftHanded(pos, pos + forward, worldUp);
        cameraRot = Utils.EulerToMatrix(owner.Transform.Rotation);
    }
    protected virtual void UpdateCamera () { }


    public virtual Ray? RaycastMouse () {
        Vector2? scenePos = mousePos_Window;
        if (scenePos is null) return null;

        Vector2 sceneSize = scenePos.Value;
        Ray ray = Raycast.ScreenPointToRay(scenePos.Value.X, scenePos.Value.Y, (int)sceneSize.X, (int)sceneSize.Y,
            Renderer.Instance.m4x4_View, Renderer.Instance.m4x4Projection);
        return ray;
    }

}
