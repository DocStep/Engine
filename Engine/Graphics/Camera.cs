using Engine.Graphics;

//using Engine.Graphics;

namespace Engine.Graphics;


public class Camera : Component {
    public Camera () {
        priority = 0 < Cameras.Count ? Cameras[0]._priority : 0;
        Cameras.Insert(0, this);
        Log.log(GetType(), _priority);
    }

    public override string Name { get; } = nameof(Camera);

    public static List<Camera> Cameras { get; private set; } = new List<Camera>();
    public static Camera? Current {
        get {
            return 0 < Cameras.Count ? Cameras[0] : null;
        }
    }


    public virtual Vector3 CameraPos => owner.Transform.Position;
    //public Matrix4x4 cameraRot = Matrix4x4.Identity;
    //public Vector2 mousePos_Window = Vector2.Zero;
    public static bool wantWarpPos = false;

    public float FOV = 60;
    public float planeNear = 0.1f;
    public float planeFar = 1000f;
    private float _priority = 0f;
    public float priority {
        get => _priority;
        set {
            if (_priority == value) return;

            _priority = value;
            int posOld = Cameras.IndexOf(this);
            int count = Cameras.Count;
            for (int i = count-1; 0 <= i; i--) {
                if (_priority < Cameras[i]._priority) {
                    Cameras.Remove(this);
                    Cameras.Insert(i, this);
                    break;
                }
            }
            Cameras.Sort((a, b) => a._priority.CompareTo(b._priority));

            //Log.log("Cameras");
            //for (int i = 0; i < count; i++) {
            //    Log.log(Cameras[i].Name, Cameras[i]._priority);
            //}
        }
    }


    public virtual void Update () { }


    public virtual Matrix4x4 GetRotationMatrix () {
        return Matrix4x4.Identity;
    }
    public virtual Matrix4x4 GetViewMatrix () {
        Vector3 pos = owner.Transform.Position;
        return Matrix4x4.CreateLookAtLeftHanded(pos, pos + owner.Transform.Forward, owner.Transform.Up);
    }

    public virtual bool RaycastMouse (out Ray ray) {
        Vector2 sceneSize = Input.Inputs.MousePos;
        Vector2 mousePos = Input.Inputs.MousePos;
        ray = Raycast.ScreenPointToRay(mousePos.X, mousePos.Y, (int)sceneSize.X, (int)sceneSize.Y,
            Renderer.Instance.m4x4_View, Renderer.Instance.m4x4Projection);
        return true;
    }

}
